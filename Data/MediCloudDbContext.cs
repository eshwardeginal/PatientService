using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PatientService.Models;

namespace PatientService.Data;

// ─────────────────────────────────────────────────────────────────────────────
//  MediCloudDbContext — EF Core entry point for Azure SQL
//  Week 2: Replaces in-memory list with real Azure SQL Database
//  Week 6: Connection string moves to Azure Key Vault (zero code changes here)
// ─────────────────────────────────────────────────────────────────────────────

public class MediCloudDbContext : DbContext
{
    public MediCloudDbContext(DbContextOptions<MediCloudDbContext> options)
        : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);

            // MRN: unique index — DB guarantees no duplicates
            entity.Property(p => p.MRN).IsRequired().HasMaxLength(20);
            entity.HasIndex(p => p.MRN).IsUnique();

            entity.Property(p => p.FullName).IsRequired().HasMaxLength(200);
            entity.Property(p => p.BloodGroup).IsRequired().HasMaxLength(5);

            // Phone: indexed — search by phone is the most common lookup
            entity.Property(p => p.Phone).IsRequired().HasMaxLength(20);
            entity.HasIndex(p => p.Phone);

            entity.Property(p => p.Email).HasMaxLength(200);
            entity.Property(p => p.Address).HasMaxLength(500);
            entity.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(p => p.UpdatedBy).HasMaxLength(100);

            // ── GLOBAL QUERY FILTER ──────────────────────────────────────────
            // Every LINQ query automatically appends WHERE IsDeleted = 0
            // No need to add .Where(p => !p.IsDeleted) anywhere in code
            entity.HasQueryFilter(p => !p.IsDeleted);

            // ── SEED DATA — same 3 demo patients as Week 1 ──────────────────
            entity.HasData(
                new Patient
                {
                    Id          = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    MRN         = "MRN-0001",
                    FullName    = "Ravi Kumar",
                    DateOfBirth = new DateTime(1985, 6, 15),
                    BloodGroup  = "O+",
                    Phone       = "+91-9876543210",
                    Email       = "ravi.kumar@example.com",
                    Address     = "123 MG Road, Mysuru, Karnataka",
                    CreatedAt   = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy   = "seed"
                },
                new Patient
                {
                    Id          = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    MRN         = "MRN-0002",
                    FullName    = "Priya Sharma",
                    DateOfBirth = new DateTime(1992, 3, 22),
                    BloodGroup  = "B+",
                    Phone       = "+91-9123456789",
                    Email       = "priya.sharma@example.com",
                    Address     = "456 Vijayanagar, Mysuru, Karnataka",
                    CreatedAt   = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy   = "seed"
                },
                new Patient
                {
                    Id          = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    MRN         = "MRN-0003",
                    FullName    = "Anil Patel",
                    DateOfBirth = new DateTime(1970, 11, 8),
                    BloodGroup  = "A-",
                    Phone       = "+91-9988776655",
                    Email       = null,
                    Address     = "789 Kuvempu Nagar, Mysuru, Karnataka",
                    CreatedAt   = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy   = "seed"
                }
            );
        });

        base.OnModelCreating(modelBuilder);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  AuditInterceptor — auto-sets CreatedAt / UpdatedAt on every SaveChanges
//  Registered in Program.cs — runs transparently before every DB write
// ─────────────────────────────────────────────────────────────────────────────

public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetAuditFields(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Patient>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
