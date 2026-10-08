using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class BloodTransaction
{
    public int Id { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public int BloodGroupId { get; set; }

    [ForeignKey(nameof(BloodGroupId))]
    public virtual BloodGroup BloodGroup { get; set; } = null!;

    public int Units { get; set; }

    public int BalanceAfter { get; set; }

    public int? DonationId { get; set; }

    [ForeignKey(nameof(DonationId))]
    public virtual Donation? Donation { get; set; }

    public int? BloodRequestId { get; set; }

    [ForeignKey(nameof(BloodRequestId))]
    public virtual BloodRequest? BloodRequest { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Remarks { get; set; }
}
