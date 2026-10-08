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
public class PatientsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PatientsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, int? bloodGroupId)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .Include(p => p.BloodGroup)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.User.FullName.ToLower().Contains(s) || 
                                     (p.User.Email != null && p.User.Email.ToLower().Contains(s)) ||
                                     p.HospitalName.ToLower().Contains(s) ||
                                     p.User.Phone.Contains(s));
        }

        if (bloodGroupId.HasValue)
        {
            query = query.Where(p => p.BloodGroupId == bloodGroupId.Value);
        }

        var patients = await query
            .OrderBy(p => p.User.FullName)
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

        return View(patients);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.BloodGroup)
            .Include(p => p.BloodRequests)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
        {
            TempData["Error"] = "Patient record not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(patient);
    }

    [HttpPost("ToggleStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
        {
            TempData["Error"] = "Patient not found.";
            return RedirectToAction(nameof(Index));
        }

        patient.IsActive = !patient.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Patient account status changed to {(patient.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }
}
