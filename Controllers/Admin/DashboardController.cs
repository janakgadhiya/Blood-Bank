using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodBankSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
[Route("Admin/[controller]/[action]")]
public class DashboardController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }
}
