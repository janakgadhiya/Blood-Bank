using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class Donor
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public int BloodGroupId { get; set; }

    [ForeignKey(nameof(BloodGroupId))]
    public virtual BloodGroup BloodGroup { get; set; } = null!;

    [Range(10, 300)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightKg { get; set; }

    public DateTime? LastDonationDate { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
}
