using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BloodBankSystem.Models;

namespace BloodBankSystem.ViewModels;

public class CreateBloodRequestViewModel
{
    [Required(ErrorMessage = "Please select the required blood group")]
    [Display(Name = "Blood Group")]
    public int BloodGroupId { get; set; }

    [Required(ErrorMessage = "Units required is required")]
    [Range(1, 50, ErrorMessage = "Requested units must be between 1 and 50")]
    [Display(Name = "Units Required")]
    public int UnitsRequired { get; set; } = 1;

    [Required(ErrorMessage = "Urgency level is required")]
    [Display(Name = "Urgency Level")]
    public RequestUrgency Urgency { get; set; } = RequestUrgency.Normal;

    [Required(ErrorMessage = "Required-by date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Required By Date")]
    public DateTime RequiredByDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Please provide a valid medical reason (5-500 characters)")]
    [Display(Name = "Medical Reason / Purpose")]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<SelectListItem>? BloodGroupList { get; set; }
    public List<StockItemViewModel>? AvailableStockSummary { get; set; }
}
