using BloodBankSystem.Models;

namespace BloodBankSystem.Services;

public class EligibilityResult
{
    public bool IsEligible { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? NextEligibleDate { get; set; }

    public static EligibilityResult Eligible() 
        => new() { IsEligible = true, Reason = "Donor is eligible to donate blood." };

    public static EligibilityResult Ineligible(string reason, DateTime? nextEligibleDate = null) 
        => new() { IsEligible = false, Reason = reason, NextEligibleDate = nextEligibleDate };
}

public interface IEligibilityService
{
    EligibilityResult CheckEligibility(Donor donor);
}

public class EligibilityService : IEligibilityService
{
    private readonly int _minAge;
    private readonly decimal _minWeightKg;
    private readonly int _donationGapDays;

    public EligibilityService(IConfiguration configuration)
    {
        _minAge = configuration.GetValue<int>("BloodBank:MinAge", 18);
        _minWeightKg = configuration.GetValue<decimal>("BloodBank:MinWeightKg", 50m);
        _donationGapDays = configuration.GetValue<int>("BloodBank:DonationGapDays", 90);
    }

    public EligibilityResult CheckEligibility(Donor donor)
    {
        if (!donor.IsActive)
        {
            return EligibilityResult.Ineligible("Donor account is currently deactivated.");
        }

        // 1. Age check
        if (donor.User != null)
        {
            var today = DateTime.Today;
            var birthDate = donor.User.DateOfBirth.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;

            if (age < _minAge)
            {
                var nextDate = birthDate.AddYears(_minAge);
                return EligibilityResult.Ineligible(
                    $"Donor must be at least {_minAge} years old (currently {age} years old).",
                    nextDate);
            }
        }

        // 2. Weight check
        if (donor.WeightKg < _minWeightKg)
        {
            return EligibilityResult.Ineligible(
                $"Donor weight must be at least {_minWeightKg} kg (currently {donor.WeightKg:F1} kg).");
        }

        // 3. Donation gap check (minimum 90 days)
        if (donor.LastDonationDate.HasValue)
        {
            var lastDate = donor.LastDonationDate.Value.Date;
            var daysSince = (DateTime.Today - lastDate).Days;
            if (daysSince < _donationGapDays)
            {
                var nextEligible = lastDate.AddDays(_donationGapDays);
                return EligibilityResult.Ineligible(
                    $"At least {_donationGapDays} days must elapse between donations ({daysSince} days elapsed).",
                    nextEligible);
            }
        }

        return EligibilityResult.Eligible();
    }
}
