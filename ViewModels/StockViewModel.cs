using System.ComponentModel.DataAnnotations;
using BloodBankSystem.Models;

namespace BloodBankSystem.ViewModels;

public class StockItemViewModel
{
    public int BloodGroupId { get; set; }
    public string BloodGroupName { get; set; } = string.Empty;
    public int AvailableUnits { get; set; }
    public int MinimumLevel { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = "Available"; // "Available", "Low", "Out of stock"
    public string StatusClass { get; set; } = "badge-available";
}

public class StockAdjustmentViewModel
{
    [Required(ErrorMessage = "Please select a blood group")]
    [Display(Name = "Blood Group")]
    public int BloodGroupId { get; set; }

    [Required(ErrorMessage = "Please specify units to adjust")]
    [Display(Name = "Units Adjustment (+ to add, - to subtract)")]
    public int UnitsDelta { get; set; }

    [Required(ErrorMessage = "Reason is required for manual stock adjustment")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 500 characters")]
    [Display(Name = "Adjustment Reason")]
    public string Reason { get; set; } = string.Empty;
}

public class StockIndexViewModel
{
    public List<StockItemViewModel> Stocks { get; set; } = new();
    public StockAdjustmentViewModel AdjustmentModel { get; set; } = new();
}
