using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class BloodStock
{
    public int Id { get; set; }

    [Required]
    public int BloodGroupId { get; set; }

    [ForeignKey(nameof(BloodGroupId))]
    public virtual BloodGroup BloodGroup { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int AvailableUnits { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumLevel { get; set; } = 10;

    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
