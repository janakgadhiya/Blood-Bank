using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers;

public class BloodSearchController : Controller
{
    private readonly ApplicationDbContext _context;

    public BloodSearchController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? bloodGroupId)
    {
        var bloodGroups = await _context.BloodGroups
            .OrderBy(b => b.Id)
            .ToListAsync();

        ViewBag.BloodGroupSelectList = bloodGroups.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.Name,
            Selected = bloodGroupId.HasValue && bloodGroupId.Value == b.Id
        }).ToList();

        StockItemViewModel? selectedGroupStock = null;
        if (bloodGroupId.HasValue)
        {
            var stock = await _context.BloodStocks
                .Include(s => s.BloodGroup)
                .FirstOrDefaultAsync(s => s.BloodGroupId == bloodGroupId.Value);

            if (stock != null)
            {
                selectedGroupStock = new StockItemViewModel
                {
                    BloodGroupId = stock.BloodGroupId,
                    BloodGroupName = stock.BloodGroup?.Name ?? "",
                    AvailableUnits = stock.AvailableUnits,
                    MinimumLevel = stock.MinimumLevel,
                    LastUpdated = stock.LastUpdated,
                    Status = stock.AvailableUnits == 0 ? "Out of stock" : (stock.AvailableUnits <= stock.MinimumLevel ? "Low" : "Available"),
                    StatusClass = stock.AvailableUnits == 0 ? "badge-out" : (stock.AvailableUnits <= stock.MinimumLevel ? "badge-low" : "badge-available")
                };
            }
        }

        return View(selectedGroupStock);
    }
}
