using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;

namespace BloodBankSystem.Services;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockOperationResult> AdjustAsync(int bloodGroupId, int unitsDelta, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return StockOperationResult.Fail("A reason is required for manual stock adjustments.");
        }

        if (unitsDelta == 0)
        {
            return StockOperationResult.Fail("Units adjustment cannot be zero.");
        }

        var stock = await _context.BloodStocks
            .Include(s => s.BloodGroup)
            .FirstOrDefaultAsync(s => s.BloodGroupId == bloodGroupId);

        if (stock == null)
        {
            return StockOperationResult.Fail("Blood group inventory record not found.");
        }

        var newUnits = stock.AvailableUnits + unitsDelta;
        if (newUnits < 0)
        {
            return StockOperationResult.Fail($"Stock cannot go below zero. Current stock is {stock.AvailableUnits} units.");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            stock.AvailableUnits = newUnits;
            stock.LastUpdated = DateTime.UtcNow;

            var transaction = new BloodTransaction
            {
                Type = TransactionType.Adjustment,
                BloodGroupId = bloodGroupId,
                Units = unitsDelta,
                BalanceAfter = newUnits,
                TransactionDate = DateTime.UtcNow,
                Remarks = reason.Trim()
            };

            _context.BloodTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return StockOperationResult.Ok($"Successfully adjusted {stock.BloodGroup?.Name ?? "Blood"} stock by {unitsDelta:+0;-0}. New balance: {newUnits} units.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return StockOperationResult.Fail($"Error processing adjustment: {ex.Message}");
        }
    }

    public async Task<StockOperationResult> AddDonationAsync(
        int donorId, 
        int bloodGroupId, 
        int units, 
        DateTime donationDate, 
        string? remarks = null, 
        int? scheduledDonationId = null)
    {
        if (units <= 0)
        {
            return StockOperationResult.Fail("Donation units must be greater than zero.");
        }

        var donor = await _context.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == donorId);

        if (donor == null || !donor.IsActive)
        {
            return StockOperationResult.Fail("Donor record not found or is currently inactive.");
        }

        var stock = await _context.BloodStocks
            .FirstOrDefaultAsync(s => s.BloodGroupId == bloodGroupId);

        if (stock == null)
        {
            return StockOperationResult.Fail("Blood group inventory record not found.");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Donation donation;
            if (scheduledDonationId.HasValue)
            {
                donation = await _context.Donations.FindAsync(scheduledDonationId.Value) 
                    ?? throw new InvalidOperationException("Scheduled donation record not found.");
                donation.Status = DonationStatus.Completed;
                donation.Units = units;
                donation.DonationDate = donationDate;
                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    donation.Remarks = remarks.Trim();
                }
            }
            else
            {
                donation = new Donation
                {
                    DonorId = donorId,
                    BloodGroupId = bloodGroupId,
                    Units = units,
                    DonationDate = donationDate,
                    Status = DonationStatus.Completed,
                    Remarks = remarks?.Trim()
                };
                _context.Donations.Add(donation);
            }

            // Save donation first to generate ID if new
            await _context.SaveChangesAsync();

            // 1. Update stock
            stock.AvailableUnits += units;
            stock.LastUpdated = DateTime.UtcNow;

            // 2. Update donor last donation date
            donor.LastDonationDate = donationDate;

            // 3. Add transaction ledger entry
            var transaction = new BloodTransaction
            {
                Type = TransactionType.DonationIn,
                BloodGroupId = bloodGroupId,
                Units = units,
                BalanceAfter = stock.AvailableUnits,
                DonationId = donation.Id,
                TransactionDate = DateTime.UtcNow,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Donation of {units} unit(s) from {donor.User?.FullName ?? "donor"}" : remarks.Trim()
            };

            _context.BloodTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return StockOperationResult.Ok($"Successfully recorded donation of {units} units. Stock balance updated to {stock.AvailableUnits}.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return StockOperationResult.Fail($"Error recording donation: {ex.Message}");
        }
    }

    public async Task<StockOperationResult> IssueRequestAsync(int requestId, string? remarks = null)
    {
        var request = await _context.BloodRequests
            .Include(r => r.BloodGroup)
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            return StockOperationResult.Fail("Blood request not found.");
        }

        if (request.Status == RequestStatus.Issued)
        {
            return StockOperationResult.Fail("This request has already been issued.");
        }

        if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Cancelled)
        {
            return StockOperationResult.Fail($"Cannot issue a request with status '{request.Status}'.");
        }

        var stock = await _context.BloodStocks
            .FirstOrDefaultAsync(s => s.BloodGroupId == request.BloodGroupId);

        if (stock == null)
        {
            return StockOperationResult.Fail("Blood group inventory record not found.");
        }

        if (stock.AvailableUnits < request.UnitsRequired)
        {
            return StockOperationResult.Fail($"Insufficient blood stock. Required: {request.UnitsRequired} units, Available: {stock.AvailableUnits} units.");
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Deduct stock
            stock.AvailableUnits -= request.UnitsRequired;
            stock.LastUpdated = DateTime.UtcNow;

            // 2. Mark request as Issued
            request.Status = RequestStatus.Issued;
            request.ProcessedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                request.AdminRemarks = string.IsNullOrWhiteSpace(request.AdminRemarks)
                    ? remarks.Trim()
                    : $"{request.AdminRemarks} | {remarks.Trim()}";
            }

            // 3. Add transaction ledger entry
            var transaction = new BloodTransaction
            {
                Type = TransactionType.IssueOut,
                BloodGroupId = request.BloodGroupId,
                Units = request.UnitsRequired,
                BalanceAfter = stock.AvailableUnits,
                BloodRequestId = request.Id,
                TransactionDate = DateTime.UtcNow,
                Remarks = $"Issued {request.UnitsRequired} unit(s) for patient {request.Patient.User.FullName} ({request.Patient.HospitalName})"
            };

            _context.BloodTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return StockOperationResult.Ok($"Request #{request.Id} successfully issued. Remaining {request.BloodGroup.Name} stock: {stock.AvailableUnits} units.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return StockOperationResult.Fail($"Error issuing request: {ex.Message}");
        }
    }

    public async Task<StockOperationResult> RejectRequestAsync(int requestId, string adminRemarks)
    {
        if (string.IsNullOrWhiteSpace(adminRemarks))
        {
            return StockOperationResult.Fail("Admin remarks are required when rejecting a blood request.");
        }

        var request = await _context.BloodRequests.FindAsync(requestId);
        if (request == null)
        {
            return StockOperationResult.Fail("Blood request not found.");
        }

        if (request.Status == RequestStatus.Issued)
        {
            return StockOperationResult.Fail("Cannot reject a request that has already been issued.");
        }

        request.Status = RequestStatus.Rejected;
        request.AdminRemarks = adminRemarks.Trim();
        request.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return StockOperationResult.Ok($"Request #{requestId} has been rejected.");
    }
}
