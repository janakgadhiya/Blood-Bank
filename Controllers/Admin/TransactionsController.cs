using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        // Fetch latest 50 transactions newest first
        var transactions = await _context.BloodTransactions
            .Include(t => t.BloodGroup)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(50)
            .ToListAsync();

        return View(transactions);
    }
}
