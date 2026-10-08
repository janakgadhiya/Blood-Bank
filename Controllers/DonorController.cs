using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.Services;

namespace BloodBankSystem.Controllers;

[Authorize(Roles = "Donor")]
public class DonorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEligibilityService _eligibilityService;

    public DonorController(
        ApplicationDbContext context,
        IEligibilityService eligibilityService)
    {
        _context = context;
        _eligibilityService = eligibilityService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        var eligibility = _eligibilityService.CheckEligibility(donor);
        var totalDonations = await _context.Donations
            .CountAsync(d => d.DonorId == donor.Id && d.Status == DonationStatus.Completed);

        var scheduledDonation = await _context.Donations
            .Where(d => d.DonorId == donor.Id && d.Status == DonationStatus.Scheduled)
            .OrderBy(d => d.DonationDate)
            .FirstOrDefaultAsync();

        ViewBag.Donor = donor;
        ViewBag.Eligibility = eligibility;
        ViewBag.TotalDonations = totalDonations;
        ViewBag.ScheduledDonation = scheduledDonation;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> MyDonations()
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        var donations = await _context.Donations
            .Include(d => d.BloodGroup)
            .Where(d => d.DonorId == donor.Id)
            .OrderByDescending(d => d.DonationDate)
            .ToListAsync();

        var eligibility = _eligibilityService.CheckEligibility(donor);
        ViewBag.Eligibility = eligibility;

        return View(donations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(DateTime donationDate, string? remarks)
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        if (donationDate.Date < DateTime.Today)
        {
            TempData["Error"] = "Donation date cannot be in the past.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Check eligibility
        var eligibility = _eligibilityService.CheckEligibility(donor);
        if (!eligibility.IsEligible)
        {
            var nextDateStr = eligibility.NextEligibleDate.HasValue 
                ? $" Next eligible date: {eligibility.NextEligibleDate.Value:yyyy-MM-dd}." 
                : "";
            TempData["Error"] = $"Cannot schedule donation: {eligibility.Reason}{nextDateStr}";
            return RedirectToAction(nameof(Dashboard));
        }

        // Check if there is already an active scheduled donation
        var hasScheduled = await _context.Donations
            .AnyAsync(d => d.DonorId == donor.Id && d.Status == DonationStatus.Scheduled);

        if (hasScheduled)
        {
            TempData["Warning"] = "You already have a scheduled donation pending.";
            return RedirectToAction(nameof(Dashboard));
        }

        var donation = new Donation
        {
            DonorId = donor.Id,
            BloodGroupId = donor.BloodGroupId,
            Units = 1,
            DonationDate = donationDate,
            Status = DonationStatus.Scheduled,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? "Scheduled via donor portal" : remarks.Trim()
        };

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Donation scheduled successfully for {donationDate:yyyy-MM-dd}.";
        return RedirectToAction(nameof(MyDonations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDonation(int donationId)
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        // Strict ownership check
        var donation = await _context.Donations
            .FirstOrDefaultAsync(d => d.Id == donationId && d.DonorId == donor.Id);

        if (donation == null)
        {
            TempData["Error"] = "Donation record not found or access denied.";
            return RedirectToAction(nameof(MyDonations));
        }

        if (donation.Status != DonationStatus.Scheduled)
        {
            TempData["Error"] = "Only scheduled donations can be cancelled.";
            return RedirectToAction(nameof(MyDonations));
        }

        donation.Status = DonationStatus.Cancelled;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Scheduled donation has been cancelled.";
        return RedirectToAction(nameof(MyDonations));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        ViewBag.BloodGroupList = await GetBloodGroupSelectListAsync();
        return View(donor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(decimal weightKg, string phone, string city, int bloodGroupId)
    {
        var donor = await GetCurrentDonorAsync();
        if (donor == null) return NotFound("Donor profile not found.");

        if (weightKg <= 0 || weightKg > 300)
        {
            ModelState.AddModelError("weightKg", "Please enter a valid weight in kg.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            ModelState.AddModelError("phone", "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            ModelState.AddModelError("city", "City is required.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BloodGroupList = await GetBloodGroupSelectListAsync();
            return View(donor);
        }

        donor.WeightKg = weightKg;
        donor.BloodGroupId = bloodGroupId;
        donor.User.Phone = phone.Trim();
        donor.User.PhoneNumber = phone.Trim();
        donor.User.City = city.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<Donor?> GetCurrentDonorAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;

        return await _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodGroup)
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }

    private async Task<List<SelectListItem>> GetBloodGroupSelectListAsync()
    {
        return await _context.BloodGroups
            .OrderBy(b => b.Id)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            })
            .ToListAsync();
    }
}
