using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BloodBankSystem.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    public virtual Donor? Donor { get; set; }
    public virtual Patient? Patient { get; set; }
}
