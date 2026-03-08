using PatientService.Models;

namespace PatientService.Repositories;

// ─────────────────────────────────────────────────────────────────────────────
// Repository Pattern (Structural Design Pattern)
// Business logic depends on this interface — NOT on SQL or any specific DB.
// Week 1: InMemoryPatientRepository implements this.
// Week 2: SqlPatientRepository implements this (Azure SQL).
// Tests:  MockPatientRepository implements this (no real DB).
// ─────────────────────────────────────────────────────────────────────────────

public interface IPatientRepository
{
    Task<Patient?>              GetByIdAsync(Guid id);
    Task<Patient?>              GetByMrnAsync(string mrn);
    Task<IEnumerable<Patient>>  GetAllAsync(int page = 1, int pageSize = 20);
    Task<IEnumerable<Patient>>  SearchAsync(string searchTerm);
    Task<Patient>               AddAsync(Patient patient);
    Task<Patient>               UpdateAsync(Patient patient);
    Task                        SoftDeleteAsync(Guid id);
    Task<bool>                  ExistsAsync(Guid id);
    Task<int>                   CountAsync();
}
