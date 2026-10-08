using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class BloodGroupsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BloodGroupsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var groups = await _context.BloodGroups
            .Include(b => b.BloodStock)
            .OrderBy(b => b.Id)
            .ToListAsync();

        return View(groups);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _context.BloodGroups.FindAsync(id);
        if (group == null)
        {
            TempData["Error"] = "Blood group not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(group);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name)
    {
        var group = await _context.BloodGroups.FindAsync(id);
        if (group == null)
        {
            TempData["Error"] = "Blood group not found.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Name", "Blood group name is required.");
            return View(group);
        }

        group.Name = name.Trim();
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Blood group updated to '{group.Name}'.";
        return RedirectToAction(nameof(Index));
    }
}
