using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stocks = await _context.BloodStocks
            .Include(s => s.BloodGroup)
            .OrderBy(s => s.BloodGroupId)
            .Select(s => new StockItemViewModel
            {
                BloodGroupId = s.BloodGroupId,
                BloodGroupName = s.BloodGroup != null ? s.BloodGroup.Name : "",
                AvailableUnits = s.AvailableUnits,
                MinimumLevel = s.MinimumLevel,
                Status = s.AvailableUnits == 0 ? "Out of stock" : (s.AvailableUnits <= s.MinimumLevel ? "Low" : "Available"),
                StatusClass = s.AvailableUnits == 0 ? "badge-out" : (s.AvailableUnits <= s.MinimumLevel ? "badge-low" : "badge-available")
            })
            .ToListAsync();

        return View(stocks);
    }

    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml");
    }
}
