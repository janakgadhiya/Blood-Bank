using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodBankSystem.Models;

public class Patient
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

    [Required]
    [StringLength(150)]
    public string HospitalName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
}
