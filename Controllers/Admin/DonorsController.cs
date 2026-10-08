using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class DonorsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, int? bloodGroupId)
    {
        var query = _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodGroup)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.User.FullName.ToLower().Contains(s) || 
                                     (d.User.Email != null && d.User.Email.ToLower().Contains(s)) ||
                                     d.User.Phone.Contains(s));
        }

        if (bloodGroupId.HasValue)
        {
            query = query.Where(d => d.BloodGroupId == bloodGroupId.Value);
        }

        var donors = await query
            .OrderBy(d => d.User.FullName)
            .ToListAsync();

        var bloodGroups = await _context.BloodGroups.OrderBy(b => b.Id).ToListAsync();
        ViewBag.BloodGroupSelectList = bloodGroups.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.Name,
            Selected = bloodGroupId.HasValue && bloodGroupId.Value == b.Id
        }).ToList();

        ViewBag.CurrentSearch = search;
        ViewBag.SelectedBloodGroupId = bloodGroupId;

        return View(donors);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var donor = await _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodGroup)
            .Include(d => d.Donations)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donor == null)
        {
            TempData["Error"] = "Donor record not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(donor);
    }

    [HttpPost("ToggleStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null)
        {
            TempData["Error"] = "Donor not found.";
            return RedirectToAction(nameof(Index));
        }

        donor.IsActive = !donor.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Donor status changed to {(donor.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }
}
