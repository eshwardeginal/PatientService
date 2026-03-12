using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Models;

namespace PatientService.Repositories;

// ─────────────────────────────────────────────────────────────────────────────
//  SqlPatientRepository — Week 2 implementation of IPatientRepository
//
//  Replaces InMemoryPatientRepository with real Azure SQL via EF Core.
//  PatientServiceImpl doesn't change AT ALL — only Program.cs registration changes.
//
//  Key differences from InMemory:
//  - Returns IQueryable → SQL is built & executed on the DB server, not in memory
//  - No lock needed — EF Core + SQL Server handles concurrency
//  - MRN generation uses DB sequence/count query
//  - Global Query Filter in DbContext handles IsDeleted automatically
// ─────────────────────────────────────────────────────────────────────────────

public class SqlPatientRepository : IPatientRepository
{
    private readonly MediCloudDbContext _ctx;

    public SqlPatientRepository(MediCloudDbContext ctx) => _ctx = ctx;

    // ── READ ─────────────────────────────────────────────────────────────────

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        // Global Query Filter already appends: WHERE IsDeleted = 0
        // EF Core generates: SELECT * FROM Patients WHERE Id = @id AND IsDeleted = 0
        return await _ctx.Patients.FindAsync(id);
    }

    public async Task<Patient?> GetByMrnAsync(string mrn)
    {
        return await _ctx.Patients
            .FirstOrDefaultAsync(p => p.MRN == mrn);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        // IQueryable → EF Core translates the entire chain to ONE SQL query
        // SELECT * FROM Patients WHERE IsDeleted=0
        // ORDER BY FullName
        // OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
        return await _ctx.Patients
            .AsNoTracking()                          // read-only — faster, no change tracking
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();

        // EF Core translates Contains() → SQL LIKE '%term%'
        // For Week 8+: replace with Azure Cognitive Search for full-text
        return await _ctx.Patients
            .AsNoTracking()
            .Where(p =>
                p.FullName.ToLower().Contains(term) ||
                p.Phone.Contains(term)              ||
                p.MRN.ToLower().Contains(term)      ||
                (p.Email != null && p.Email.ToLower().Contains(term)))
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    // ── WRITE ────────────────────────────────────────────────────────────────

    public async Task<Patient> AddAsync(Patient patient)
    {
        // Generate MRN using current DB count
        // Week 3+: consider a dedicated DB sequence for true concurrency safety
        var count = await _ctx.Patients
            .IgnoreQueryFilters()                    // count ALL including deleted
            .CountAsync();

        patient.MRN = $"MRN-{(count + 1):D4}";

        await _ctx.Patients.AddAsync(patient);
        await _ctx.SaveChangesAsync();               // AuditInterceptor sets CreatedAt here

        return patient;
    }

    public async Task<Patient> UpdateAsync(Patient patient)
    {
        _ctx.Patients.Update(patient);
        await _ctx.SaveChangesAsync();               // AuditInterceptor sets UpdatedAt here
        return patient;
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var patient = await _ctx.Patients.FindAsync(id);
        if (patient is null) return;

        patient.IsDeleted = true;
        patient.UpdatedAt = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();
    }

    // ── UTILITY ──────────────────────────────────────────────────────────────

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _ctx.Patients.AnyAsync(p => p.Id == id);
    }

    public async Task<int> CountAsync()
    {
        return await _ctx.Patients.CountAsync();    // Global filter: excludes deleted
    }
}
