using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class Donation
{
    public int Id { get; set; }

    [Required]
    public int DonorId { get; set; }

    [ForeignKey(nameof(DonorId))]
    public virtual Donor Donor { get; set; } = null!;

    [Required]
    public int BloodGroupId { get; set; }

    [ForeignKey(nameof(BloodGroupId))]
    public virtual BloodGroup BloodGroup { get; set; } = null!;

    [Range(1, 10)]
    public int Units { get; set; }

    [Required]
    public DateTime DonationDate { get; set; }

    [Required]
    public DonationStatus Status { get; set; } = DonationStatus.Scheduled;

    [StringLength(500)]
    public string? Remarks { get; set; }
}
