using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Services;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class StockController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stockService;
    private readonly IConfiguration _configuration;

    public StockController(
        ApplicationDbContext context,
        IStockService stockService,
        IConfiguration configuration)
    {
        _context = context;
        _stockService = stockService;
        _configuration = configuration;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var model = await BuildStockIndexViewModelAsync();
        return View(model);
    }

    [HttpPost("Adjust")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel adjustmentModel)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildStockIndexViewModelAsync();
            model.AdjustmentModel = adjustmentModel;
            return View(nameof(Index), model);
        }

        var result = await _stockService.AdjustAsync(
            adjustmentModel.BloodGroupId,
            adjustmentModel.UnitsDelta,
            adjustmentModel.Reason);

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

    [HttpPost("UpdateMinimumLevel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMinimumLevel(int bloodGroupId, int minimumLevel)
    {
        if (minimumLevel < 0)
        {
            TempData["Error"] = "Minimum level cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        var stock = await _context.BloodStocks.FirstOrDefaultAsync(s => s.BloodGroupId == bloodGroupId);
        if (stock != null)
        {
            stock.MinimumLevel = minimumLevel;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Minimum alert level updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<StockIndexViewModel> BuildStockIndexViewModelAsync()
    {
        var lowStockDefault = _configuration.GetValue<int>("BloodBank:LowStockLevel", 10);

        var stocks = await _context.BloodStocks
            .Include(s => s.BloodGroup)
            .OrderBy(s => s.BloodGroupId)
            .ToListAsync();

        var stockItems = stocks.Select(s =>
        {
            string status;
            string statusClass;

            if (s.AvailableUnits == 0)
            {
                status = "Out of stock";
                statusClass = "badge-out";
            }
            else if (s.AvailableUnits <= (s.MinimumLevel > 0 ? s.MinimumLevel : lowStockDefault))
            {
                status = "Low";
                statusClass = "badge-low";
            }
            else
            {
                status = "Available";
                statusClass = "badge-available";
            }

            return new StockItemViewModel
            {
                BloodGroupId = s.BloodGroupId,
                BloodGroupName = s.BloodGroup?.Name ?? "Unknown",
                AvailableUnits = s.AvailableUnits,
                MinimumLevel = s.MinimumLevel,
                LastUpdated = s.LastUpdated,
                Status = status,
                StatusClass = statusClass
            };
        }).ToList();

        ViewBag.BloodGroupSelectList = stocks.Select(s => new SelectListItem
        {
            Value = s.BloodGroupId.ToString(),
            Text = s.BloodGroup?.Name ?? ""
        }).ToList();

        return new StockIndexViewModel
        {
            Stocks = stockItems,
            AdjustmentModel = new StockAdjustmentViewModel()
        };
    }
}
