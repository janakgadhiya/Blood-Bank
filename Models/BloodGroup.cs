using System.ComponentModel.DataAnnotations;

namespace BloodBankSystem.Models;

public class BloodGroup
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Name { get; set; } = string.Empty;

    public virtual BloodStock? BloodStock { get; set; }
    public virtual ICollection<Donor> Donors { get; set; } = new List<Donor>();
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
    public virtual ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
    public virtual ICollection<BloodTransaction> BloodTransactions { get; set; } = new List<BloodTransaction>();
}
