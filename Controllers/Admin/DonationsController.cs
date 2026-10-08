using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.Services;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class DonationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stockService;
    private readonly IEligibilityService _eligibilityService;

    public DonationsController(
        ApplicationDbContext context,
        IStockService stockService,
        IEligibilityService eligibilityService)
    {
        _context = context;
        _stockService = stockService;
        _eligibilityService = eligibilityService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(DonationStatus? status = null)
    {
        var query = _context.Donations
            .Include(d => d.Donor)
                .ThenInclude(dn => dn.User)
            .Include(d => d.BloodGroup)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        var donations = await query
            .OrderByDescending(d => d.DonationDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        ViewBag.SelectedStatus = status;
        return View(donations);
    }

    [HttpGet("Record")]
    public async Task<IActionResult> Record()
    {
        var model = new RecordDonationViewModel
        {
            DonorList = await GetActiveDonorsSelectListAsync()
        };
        return View(model);
    }

    [HttpPost("Record")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(RecordDonationViewModel model)
    {
        var donor = await _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodGroup)
            .FirstOrDefaultAsync(d => d.Id == model.DonorId);

        if (donor == null)
        {
            ModelState.AddModelError("DonorId", "Selected donor not found.");
        }
        else
        {
            // Eligibility Check
            var eligibility = _eligibilityService.CheckEligibility(donor);
            if (!eligibility.IsEligible)
            {
                var nextDateStr = eligibility.NextEligibleDate.HasValue 
                    ? $" Next eligible donation date: {eligibility.NextEligibleDate.Value:yyyy-MM-dd}." 
                    : "";
                ModelState.AddModelError(string.Empty, $"Donor Ineligible: {eligibility.Reason}{nextDateStr}");
            }
        }

        if (!ModelState.IsValid)
        {
            model.DonorList = await GetActiveDonorsSelectListAsync();
            return View(model);
        }

        var result = await _stockService.AddDonationAsync(
            donor!.Id,
            donor.BloodGroupId,
            model.Units,
            model.DonationDate,
            model.Remarks);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Message;
        model.DonorList = await GetActiveDonorsSelectListAsync();
        return View(model);
    }

    [HttpPost("Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int donationId, int units, string? remarks)
    {
        if (units <= 0)
        {
            TempData["Error"] = "Units collected must be greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var donation = await _context.Donations
            .Include(d => d.Donor)
                .ThenInclude(dn => dn.User)
            .FirstOrDefaultAsync(d => d.Id == donationId);

        if (donation == null)
        {
            TempData["Error"] = "Donation record not found.";
            return RedirectToAction(nameof(Index));
        }

        if (donation.Status != DonationStatus.Scheduled)
        {
            TempData["Error"] = $"Only scheduled donations can be completed. Current status: {donation.Status}.";
            return RedirectToAction(nameof(Index));
        }

        // Check eligibility
        var eligibility = _eligibilityService.CheckEligibility(donation.Donor);
        if (!eligibility.IsEligible)
        {
            TempData["Error"] = $"Cannot complete donation: {eligibility.Reason}";
            return RedirectToAction(nameof(Index));
        }

        var result = await _stockService.AddDonationAsync(
            donation.DonorId,
            donation.BloodGroupId,
            units,
            DateTime.UtcNow,
            remarks,
            scheduledDonationId: donationId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int donationId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A rejection reason is required.";
            return RedirectToAction(nameof(Index));
        }

        var donation = await _context.Donations.FindAsync(donationId);
        if (donation == null)
        {
            TempData["Error"] = "Donation record not found.";
            return RedirectToAction(nameof(Index));
        }

        if (donation.Status == DonationStatus.Completed)
        {
            TempData["Error"] = "Cannot reject a completed donation.";
            return RedirectToAction(nameof(Index));
        }

        donation.Status = DonationStatus.Rejected;
        donation.Remarks = $"Rejected: {reason.Trim()}";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Donation rejected successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int donationId)
    {
        var donation = await _context.Donations.FindAsync(donationId);
        if (donation == null)
        {
            TempData["Error"] = "Donation record not found.";
            return RedirectToAction(nameof(Index));
        }

        if (donation.Status == DonationStatus.Completed)
        {
            TempData["Error"] = "Cannot cancel an already completed donation.";
            return RedirectToAction(nameof(Index));
        }

        donation.Status = DonationStatus.Cancelled;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Donation cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetActiveDonorsSelectListAsync()
    {
        return await _context.Donors
            .Where(d => d.IsActive)
            .Include(d => d.User)
            .Include(d => d.BloodGroup)
            .OrderBy(d => d.User.FullName)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User.FullName} ({d.BloodGroup.Name}) - Weight: {d.WeightKg:F1}kg"
            })
            .ToListAsync();
    }
}
