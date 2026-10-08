using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BloodBankSystem.Models;

namespace BloodBankSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BloodGroup> BloodGroups => Set<BloodGroup>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<BloodStock> BloodStocks => Set<BloodStock>();
    public DbSet<BloodRequest> BloodRequests => Set<BloodRequest>();
    public DbSet<BloodTransaction> BloodTransactions => Set<BloodTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique index on BloodStock.BloodGroupId
        builder.Entity<BloodStock>()
            .HasIndex(s => s.BloodGroupId)
            .IsUnique();

        // One-to-one relationship between BloodGroup and BloodStock
        builder.Entity<BloodStock>()
            .HasOne(s => s.BloodGroup)
            .WithOne(b => b.BloodStock)
            .HasForeignKey<BloodStock>(s => s.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-one relationship between ApplicationUser and Donor
        builder.Entity<Donor>()
            .HasOne(d => d.User)
            .WithOne(u => u.Donor)
            .HasForeignKey<Donor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-one relationship between ApplicationUser and Patient
        builder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict delete on all foreign keys to maintain data integrity
        builder.Entity<Donor>()
            .HasOne(d => d.BloodGroup)
            .WithMany(b => b.Donors)
            .HasForeignKey(d => d.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Patient>()
            .HasOne(p => p.BloodGroup)
            .WithMany(b => b.Patients)
            .HasForeignKey(p => p.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Donation>()
            .HasOne(d => d.Donor)
            .WithMany(dn => dn.Donations)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Donation>()
            .HasOne(d => d.BloodGroup)
            .WithMany(b => b.Donations)
            .HasForeignKey(d => d.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BloodRequest>()
            .HasOne(r => r.Patient)
            .WithMany(p => p.BloodRequests)
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BloodRequest>()
            .HasOne(r => r.BloodGroup)
            .WithMany(b => b.BloodRequests)
            .HasForeignKey(r => r.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BloodTransaction>()
            .HasOne(t => t.BloodGroup)
            .WithMany(b => b.BloodTransactions)
            .HasForeignKey(t => t.BloodGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BloodTransaction>()
            .HasOne(t => t.Donation)
            .WithMany()
            .HasForeignKey(t => t.DonationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BloodTransaction>()
            .HasOne(t => t.BloodRequest)
            .WithMany()
            .HasForeignKey(t => t.BloodRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
