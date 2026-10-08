using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BloodBankSystem.Models;

namespace BloodBankSystem.ViewModels;

public class RecordDonationViewModel
{
    [Required(ErrorMessage = "Please select a donor")]
    [Display(Name = "Donor")]
    public int DonorId { get; set; }

    [Required(ErrorMessage = "Please specify units donated")]
    [Range(1, 10, ErrorMessage = "Donation units must be between 1 and 10")]
    [Display(Name = "Units Donated")]
    public int Units { get; set; } = 1;

    [Required(ErrorMessage = "Donation date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Donation Date")]
    public DateTime DonationDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Remarks / Notes")]
    public string? Remarks { get; set; }

    public IEnumerable<SelectListItem>? DonorList { get; set; }
}

public class CompleteScheduledDonationViewModel
{
    public int DonationId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string BloodGroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Units collected is required")]
    [Range(1, 10, ErrorMessage = "Units must be between 1 and 10")]
    public int Units { get; set; } = 1;

    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class RejectDonationViewModel
{
    public int DonationId { get; set; }

    [Required(ErrorMessage = "Reason is required to reject a donation")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
