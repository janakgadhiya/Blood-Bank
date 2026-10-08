using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodBankSystem.Controllers;

[Authorize(Roles = "Patient")]
public class PatientController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult NewRequest()
    {
        return View();
    }

    public IActionResult MyRequests()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }
}
