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

                // Create initial BloodStock for this blood group
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
                // Ensure BloodStock exists for existing blood group
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
    }
}
