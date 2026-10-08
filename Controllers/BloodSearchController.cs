using Microsoft.AspNetCore.Mvc;

namespace BloodBankSystem.Controllers;

public class BloodSearchController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
