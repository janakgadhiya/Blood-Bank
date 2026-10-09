using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.Services;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stockService;

    public RequestsController(ApplicationDbContext context, IStockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        // Simple and normal list of blood requests, newest first
        var requests = await _context.BloodRequests
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .Include(r => r.BloodGroup)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        // Fetch live stocks to display alongside requests
        var stocks = await _context.BloodStocks.ToDictionaryAsync(s => s.BloodGroupId, s => s.AvailableUnits);
        ViewBag.StockMap = stocks;

        return View(requests);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _context.BloodRequests
            .Include(r => r.BloodGroup)
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            TempData["Error"] = "Blood request not found.";
            return RedirectToAction(nameof(Index));
        }

        var stock = await _context.BloodStocks
            .FirstOrDefaultAsync(s => s.BloodGroupId == request.BloodGroupId);

        ViewBag.LiveStock = stock;
        return View(request);
    }

    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.BloodRequests
            .Include(r => r.BloodGroup)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            TempData["Error"] = "Request not found.";
            return RedirectToAction(nameof(Index));
        }

        if (request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = $"Cannot approve a request with status '{request.Status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var stock = await _context.BloodStocks
            .FirstOrDefaultAsync(s => s.BloodGroupId == request.BloodGroupId);

        if (stock == null || stock.AvailableUnits < request.UnitsRequired)
        {
            var available = stock?.AvailableUnits ?? 0;
            TempData["Error"] = $"Cannot approve request: Insufficient stock. Available: {available} units, Requested: {request.UnitsRequired} units.";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = RequestStatus.Approved;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Request #{id} approved successfully. Stock reserved for issuance.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Issue")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int id, string? remarks)
    {
        var result = await _stockService.IssueRequestAsync(id, remarks);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string adminRemarks)
    {
        if (string.IsNullOrWhiteSpace(adminRemarks))
        {
            TempData["Error"] = "Admin remarks are required to reject a blood request.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _stockService.RejectRequestAsync(id, adminRemarks);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
