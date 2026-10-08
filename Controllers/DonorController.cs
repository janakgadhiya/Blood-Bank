using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodBankSystem.Controllers;

[Authorize(Roles = "Donor")]
public class DonorController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult MyDonations()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }
}
