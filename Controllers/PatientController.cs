using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Data;
using BloodBankSystem.Models;
using BloodBankSystem.ViewModels;

namespace BloodBankSystem.Controllers;

[Authorize(Roles = "Patient")]
public class PatientController : Controller
{
    private readonly ApplicationDbContext _context;

    public PatientController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        var requests = await _context.BloodRequests
            .Where(r => r.PatientId == patient.Id)
            .ToListAsync();

        ViewBag.PendingCount = requests.Count(r => r.Status == RequestStatus.Pending);
        ViewBag.ApprovedCount = requests.Count(r => r.Status == RequestStatus.Approved);
        ViewBag.IssuedCount = requests.Count(r => r.Status == RequestStatus.Issued);
        ViewBag.RejectedCount = requests.Count(r => r.Status == RequestStatus.Rejected);
        ViewBag.Patient = patient;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> NewRequest()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        var model = new CreateBloodRequestViewModel
        {
            BloodGroupId = patient.BloodGroupId,
            RequiredByDate = DateTime.Today.AddDays(1),
            BloodGroupList = await GetBloodGroupSelectListAsync(),
            AvailableStockSummary = await GetStockSummaryAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewRequest(CreateBloodRequestViewModel model)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        if (model.RequiredByDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("RequiredByDate", "Required-by date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            model.BloodGroupList = await GetBloodGroupSelectListAsync();
            model.AvailableStockSummary = await GetStockSummaryAsync();
            return View(model);
        }

        var request = new BloodRequest
        {
            PatientId = patient.Id,
            BloodGroupId = model.BloodGroupId,
            UnitsRequired = model.UnitsRequired,
            Urgency = model.Urgency,
            RequiredByDate = model.RequiredByDate,
            Reason = model.Reason.Trim(),
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.BloodRequests.Add(request);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Blood request submitted successfully. Our team will review it promptly.";
        return RedirectToAction(nameof(MyRequests));
    }

    [HttpGet]
    public async Task<IActionResult> MyRequests()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        var requests = await _context.BloodRequests
            .Include(r => r.BloodGroup)
            .Where(r => r.PatientId == patient.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        // Strict ownership check
        var request = await _context.BloodRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patient.Id);

        if (request == null)
        {
            TempData["Error"] = "Request not found or you are not authorized to cancel this request.";
            return RedirectToAction(nameof(MyRequests));
        }

        if (request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = $"Only pending requests can be cancelled. Current status is {request.Status}.";
            return RedirectToAction(nameof(MyRequests));
        }

        request.Status = RequestStatus.Cancelled;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Blood request #{id} has been cancelled.";
        return RedirectToAction(nameof(MyRequests));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        ViewBag.BloodGroupList = await GetBloodGroupSelectListAsync();
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string hospitalName, string phone, string city, int bloodGroupId)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound("Patient profile not found.");

        if (string.IsNullOrWhiteSpace(hospitalName))
        {
            ModelState.AddModelError("hospitalName", "Hospital Name is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            ModelState.AddModelError("city", "City is required.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            ModelState.AddModelError("phone", "Phone is required.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BloodGroupList = await GetBloodGroupSelectListAsync();
            return View(patient);
        }

        patient.HospitalName = hospitalName.Trim();
        patient.BloodGroupId = bloodGroupId;
        patient.User.City = city.Trim();
        patient.User.Phone = phone.Trim();
        patient.User.PhoneNumber = phone.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<Patient?> GetCurrentPatientAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;

        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.BloodGroup)
            .FirstOrDefaultAsync(p => p.UserId == userId);
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

    private async Task<List<StockItemViewModel>> GetStockSummaryAsync()
    {
        return await _context.BloodStocks
            .Include(s => s.BloodGroup)
            .OrderBy(s => s.BloodGroupId)
            .Select(s => new StockItemViewModel
            {
                BloodGroupId = s.BloodGroupId,
                BloodGroupName = s.BloodGroup != null ? s.BloodGroup.Name : "",
                AvailableUnits = s.AvailableUnits,
                Status = s.AvailableUnits == 0 ? "Out of stock" : (s.AvailableUnits <= s.MinimumLevel ? "Low" : "Available"),
                StatusClass = s.AvailableUnits == 0 ? "badge-out" : (s.AvailableUnits <= s.MinimumLevel ? "badge-low" : "badge-available")
            })
            .ToListAsync();
    }
}
