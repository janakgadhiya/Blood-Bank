using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BloodBankSystem.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of Birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-20);

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(20)]
    public string Gender { get; set; } = "Male";

    [Required(ErrorMessage = "Please select a role")]
    [Display(Name = "Register As")]
    public string Role { get; set; } = "Donor"; // "Donor" or "Patient"

    [Required(ErrorMessage = "Please select your blood group")]
    [Display(Name = "Blood Group")]
    public int BloodGroupId { get; set; }

    // Donor-specific field
    [Display(Name = "Weight (kg)")]
    [Range(10, 300, ErrorMessage = "Please enter a realistic weight in kg")]
    public decimal? WeightKg { get; set; }

    // Patient-specific field
    [Display(Name = "Hospital Name")]
    [StringLength(150)]
    public string? HospitalName { get; set; }

    public IEnumerable<SelectListItem>? BloodGroupList { get; set; }
}
