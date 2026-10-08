using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Models;

namespace BloodBankSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // 1. Seed Roles: Admin, Donor, Patient
        string[] roles = ["Admin", "Donor", "Patient"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Admin User
        var adminEmail = "admin@bloodbank.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                Phone = "1234567890",
                PhoneNumber = "1234567890",
                City = "Central City",
                DateOfBirth = new DateTime(1985, 1, 1),
                Gender = "Male"
            };

            var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Seed 8 Blood Groups with initial BloodStock (0 units)
        string[] bloodGroupNames = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];
        foreach (var groupName in bloodGroupNames)
        {
            var bloodGroup = await context.BloodGroups.FirstOrDefaultAsync(b => b.Name == groupName);
            if (bloodGroup == null)
            {
                bloodGroup = new BloodGroup { Name = groupName };
                context.BloodGroups.Add(bloodGroup);
                await context.SaveChangesAsync();

                var stock = new BloodStock
                {
                    BloodGroupId = bloodGroup.Id,
                    AvailableUnits = 0,
                    MinimumLevel = 10,
                    LastUpdated = DateTime.UtcNow
                };
                context.BloodStocks.Add(stock);
                await context.SaveChangesAsync();
            }
            else
            {
                var stock = await context.BloodStocks.FirstOrDefaultAsync(s => s.BloodGroupId == bloodGroup.Id);
                if (stock == null)
                {
                    stock = new BloodStock
                    {
                        BloodGroupId = bloodGroup.Id,
                        AvailableUnits = 0,
                        MinimumLevel = 10,
                        LastUpdated = DateTime.UtcNow
                    };
                    context.BloodStocks.Add(stock);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 4. Seed Demo Data (guarded by configuration)
        var shouldSeedDemo = configuration.GetValue<bool>("BloodBank:SeedDemoData", true);
        if (shouldSeedDemo)
        {
            await SeedDemoRecordsAsync(context, userManager);
        }
    }

    private static async Task SeedDemoRecordsAsync(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager)
    {
        // Don't re-seed if demo donors already exist
        if (await context.Donors.AnyAsync())
        {
            return;
        }

        var groups = await context.BloodGroups.ToDictionaryAsync(b => b.Name, b => b.Id);

        // --- Demo Donors ---
        // Donor 1: Alice Smith (O+, eligible, last donated 120 days ago)
        var user1 = new ApplicationUser
        {
            UserName = "donor1@bloodbank.com",
            Email = "donor1@bloodbank.com",
            EmailConfirmed = true,
            FullName = "Alice Smith",
            Phone = "555-1001",
            PhoneNumber = "555-1001",
            City = "New York",
            DateOfBirth = new DateTime(1994, 3, 15),
            Gender = "Female"
        };
        await userManager.CreateAsync(user1, "Donor@123");
        await userManager.AddToRoleAsync(user1, "Donor");

        var donor1 = new Donor
        {
            UserId = user1.Id,
            BloodGroupId = groups["O+"],
            WeightKg = 64.0m,
            LastDonationDate = DateTime.Today.AddDays(-120),
            IsActive = true
        };
        context.Donors.Add(donor1);

        // Donor 2: Bob Jones (A+, recently donated 20 days ago - ineligible by gap)
        var user2 = new ApplicationUser
        {
            UserName = "donor2@bloodbank.com",
            Email = "donor2@bloodbank.com",
            EmailConfirmed = true,
            FullName = "Bob Jones",
            Phone = "555-1002",
            PhoneNumber = "555-1002",
            City = "Boston",
            DateOfBirth = new DateTime(1989, 7, 22),
            Gender = "Male"
        };
        await userManager.CreateAsync(user2, "Donor@123");
        await userManager.AddToRoleAsync(user2, "Donor");

        var donor2 = new Donor
        {
            UserId = user2.Id,
            BloodGroupId = groups["A+"],
            WeightKg = 76.5m,
            LastDonationDate = DateTime.Today.AddDays(-20),
            IsActive = true
        };
        context.Donors.Add(donor2);

        // Donor 3: Charlie Brown (B-, underweight 47kg - ineligible by weight)
        var user3 = new ApplicationUser
        {
            UserName = "donor3@bloodbank.com",
            Email = "donor3@bloodbank.com",
            EmailConfirmed = true,
            FullName = "Charlie Brown",
            Phone = "555-1003",
            PhoneNumber = "555-1003",
            City = "Chicago",
            DateOfBirth = new DateTime(2001, 10, 5),
            Gender = "Male"
        };
        await userManager.CreateAsync(user3, "Donor@123");
        await userManager.AddToRoleAsync(user3, "Donor");

        var donor3 = new Donor
        {
            UserId = user3.Id,
            BloodGroupId = groups["B-"],
            WeightKg = 47.0m,
            LastDonationDate = null,
            IsActive = true
        };
        context.Donors.Add(donor3);

        await context.SaveChangesAsync();

        // --- Demo Patients ---
        // Patient 1: David Wilson (O+)
        var patientUser1 = new ApplicationUser
        {
            UserName = "patient1@bloodbank.com",
            Email = "patient1@bloodbank.com",
            EmailConfirmed = true,
            FullName = "David Wilson",
            Phone = "555-2001",
            PhoneNumber = "555-2001",
            City = "New York",
            DateOfBirth = new DateTime(1978, 5, 12),
            Gender = "Male"
        };
        await userManager.CreateAsync(patientUser1, "Patient@123");
        await userManager.AddToRoleAsync(patientUser1, "Patient");

        var patient1 = new Patient
        {
            UserId = patientUser1.Id,
            BloodGroupId = groups["O+"],
            HospitalName = "St. Jude Memorial Hospital (Room 402)",
            IsActive = true
        };
        context.Patients.Add(patient1);

        // Patient 2: Emma Davis (AB-)
        var patientUser2 = new ApplicationUser
        {
            UserName = "patient2@bloodbank.com",
            Email = "patient2@bloodbank.com",
            EmailConfirmed = true,
            FullName = "Emma Davis",
            Phone = "555-2002",
            PhoneNumber = "555-2002",
            City = "Boston",
            DateOfBirth = new DateTime(1992, 9, 30),
            Gender = "Female"
        };
        await userManager.CreateAsync(patientUser2, "Patient@123");
        await userManager.AddToRoleAsync(patientUser2, "Patient");

        var patient2 = new Patient
        {
            UserId = patientUser2.Id,
            BloodGroupId = groups["AB-"],
            HospitalName = "Metropolitan Medical Center",
            IsActive = true
        };
        context.Patients.Add(patient2);

        await context.SaveChangesAsync();

        // --- Completed Donations with Matching Stock & Ledger Transactions ---
        // 1. O+ Donation (2 units from Alice Smith, 120 days ago)
        var donation1 = new Donation
        {
            DonorId = donor1.Id,
            BloodGroupId = groups["O+"],
            Units = 2,
            DonationDate = DateTime.Today.AddDays(-120),
            Status = DonationStatus.Completed,
            Remarks = "Routine whole blood donation"
        };
        context.Donations.Add(donation1);
        await context.SaveChangesAsync();

        var stockOPlus = await context.BloodStocks.FirstAsync(s => s.BloodGroupId == groups["O+"]);
        stockOPlus.AvailableUnits += 2;
        stockOPlus.LastUpdated = DateTime.UtcNow;

        var tx1 = new BloodTransaction
        {
            Type = TransactionType.DonationIn,
            BloodGroupId = groups["O+"],
            Units = 2,
            BalanceAfter = stockOPlus.AvailableUnits,
            DonationId = donation1.Id,
            TransactionDate = DateTime.UtcNow.AddDays(-120),
            Remarks = "Initial donation stock from Alice Smith"
        };
        context.BloodTransactions.Add(tx1);

        // 2. A+ Donation (3 units from Bob Jones, 20 days ago)
        var donation2 = new Donation
        {
            DonorId = donor2.Id,
            BloodGroupId = groups["A+"],
            Units = 3,
            DonationDate = DateTime.Today.AddDays(-20),
            Status = DonationStatus.Completed,
            Remarks = "Emergency community blood drive donation"
        };
        context.Donations.Add(donation2);
        await context.SaveChangesAsync();

        var stockAPlus = await context.BloodStocks.FirstAsync(s => s.BloodGroupId == groups["A+"]);
        stockAPlus.AvailableUnits += 3;
        stockAPlus.LastUpdated = DateTime.UtcNow;

        var tx2 = new BloodTransaction
        {
            Type = TransactionType.DonationIn,
            BloodGroupId = groups["A+"],
            Units = 3,
            BalanceAfter = stockAPlus.AvailableUnits,
            DonationId = donation2.Id,
            TransactionDate = DateTime.UtcNow.AddDays(-20),
            Remarks = "Initial donation stock from Bob Jones"
        };
        context.BloodTransactions.Add(tx2);

        // --- 2 Pending Requests (including one Critical) ---
        // Request 1: Critical for Patient 1 (O+, 2 units)
        var req1 = new BloodRequest
        {
            PatientId = patient1.Id,
            BloodGroupId = groups["O+"],
            UnitsRequired = 2,
            Urgency = RequestUrgency.Critical,
            RequiredByDate = DateTime.Today.AddDays(1),
            Reason = "Emergency trauma surgery - acute hemorrhage requiring immediate transfusion",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };
        context.BloodRequests.Add(req1);

        // Request 2: Normal for Patient 2 (AB-, 1 unit)
        var req2 = new BloodRequest
        {
            PatientId = patient2.Id,
            BloodGroupId = groups["AB-"],
            UnitsRequired = 1,
            Urgency = RequestUrgency.Normal,
            RequiredByDate = DateTime.Today.AddDays(4),
            Reason = "Scheduled elective orthopedic hip replacement surgery",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.BloodRequests.Add(req2);

        await context.SaveChangesAsync();
    }
}
