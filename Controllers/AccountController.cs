using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard();
        }

        var model = new RegisterViewModel
        {
            BloodGroupList = await GetBloodGroupSelectListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Donor" && (!model.WeightKg.HasValue || model.WeightKg <= 0))
        {
            ModelState.AddModelError("WeightKg", "Weight is required for donor registration.");
        }
        else if (model.Role == "Patient" && string.IsNullOrWhiteSpace(model.HospitalName))
        {
            ModelState.AddModelError("HospitalName", "Hospital Name is required for patient registration.");
        }

        if (!ModelState.IsValid)
        {
            model.BloodGroupList = await GetBloodGroupSelectListAsync();
            return View(model);
        }

        // Check if email already registered
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "An account with this email address already exists.");
            model.BloodGroupList = await GetBloodGroupSelectListAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Phone = model.Phone,
            PhoneNumber = model.Phone,
            City = model.City,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Assign selected role
            await _userManager.AddToRoleAsync(user, model.Role);

            // Create Donor or Patient profile
            if (model.Role == "Donor")
            {
                var donor = new Donor
                {
                    UserId = user.Id,
                    BloodGroupId = model.BloodGroupId,
                    WeightKg = model.WeightKg ?? 50m,
                    LastDonationDate = null,
                    IsActive = true
                };
                _context.Donors.Add(donor);
            }
            else if (model.Role == "Patient")
            {
                var patient = new Patient
                {
                    UserId = user.Id,
                    BloodGroupId = model.BloodGroupId,
                    HospitalName = model.HospitalName?.Trim() ?? string.Empty,
                    IsActive = true
                };
                _context.Patients.Add(patient);
            }

            await _context.SaveChangesAsync();

            // Sign user in
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = "Registration successful! Welcome to BloodBank.";

            return RedirectToDashboard();
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.BloodGroupList = await GetBloodGroupSelectListAsync();
        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Welcome back, {user.FullName}!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToDashboard();
        }

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToDashboard()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Dashboard");
        }
        if (User.IsInRole("Donor"))
        {
            return RedirectToAction("Dashboard", "Donor");
        }
        if (User.IsInRole("Patient"))
        {
            return RedirectToAction("Dashboard", "Patient");
        }
        return RedirectToAction("Index", "Home");
    }

    private async Task<List<SelectListItem>> GetBloodGroupSelectListAsync()
    {
        return await _context.BloodGroups
            .OrderBy(b => b.Id)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            })
            .ToListAsync();
    }
}
