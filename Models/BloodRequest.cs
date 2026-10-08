using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class BloodRequest
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual Patient Patient { get; set; } = null!;

    [Required]
    public int BloodGroupId { get; set; }

    [ForeignKey(nameof(BloodGroupId))]
    public virtual BloodGroup BloodGroup { get; set; } = null!;

    [Range(1, 50)]
    public int UnitsRequired { get; set; }

    [Required]
    public RequestUrgency Urgency { get; set; } = RequestUrgency.Normal;

    [Required]
    public DateTime RequiredByDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [StringLength(500)]
    public string? AdminRemarks { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
}
