using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        // 1. Metrics for Number Cards
        var totalDonors = await _context.Donors.CountAsync(d => d.IsActive);
        var totalPatients = await _context.Patients.CountAsync(p => p.IsActive);
        var totalAvailableUnits = await _context.BloodStocks.SumAsync(s => s.AvailableUnits);
        
        var pendingRequests = await _context.BloodRequests
            .CountAsync(r => r.Status == RequestStatus.Pending);

        var urgentAndCriticalPending = await _context.BloodRequests
            .CountAsync(r => r.Status == RequestStatus.Pending && 
                (r.Urgency == RequestUrgency.Urgent || r.Urgency == RequestUrgency.Critical));

        var donationsToday = await _context.Donations
            .CountAsync(d => d.Status == DonationStatus.Completed && d.DonationDate.Date == today);

        // 2. Low Stock Groups (AvailableUnits <= MinimumLevel)
        var lowStockGroups = await _context.BloodStocks
            .Include(s => s.BloodGroup)
            .Where(s => s.AvailableUnits <= s.MinimumLevel)
            .OrderBy(s => s.AvailableUnits)
            .ToListAsync();

        // 3. 5 Latest Blood Requests
        var latestRequests = await _context.BloodRequests
            .Include(r => r.BloodGroup)
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalDonors = totalDonors;
        ViewBag.TotalPatients = totalPatients;
        ViewBag.TotalAvailableUnits = totalAvailableUnits;
        ViewBag.PendingRequests = pendingRequests;
        ViewBag.UrgentAndCriticalPending = urgentAndCriticalPending;
        ViewBag.DonationsToday = donationsToday;
        ViewBag.LowStockGroups = lowStockGroups;
        ViewBag.LatestRequests = latestRequests;

        return View();
    }
}
