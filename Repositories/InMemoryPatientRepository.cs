using PatientService.Models;

namespace PatientService.Repositories;

// ─────────────────────────────────────────────────────────────────────────────
// Week 1: In-Memory implementation — no Azure SQL needed yet.
// Week 2: You will ADD SqlPatientRepository and swap in Program.cs.
//         This file stays for unit tests.
// ─────────────────────────────────────────────────────────────────────────────

public class InMemoryPatientRepository : IPatientRepository
{
    private readonly List<Patient> _store = new();
    private readonly object        _lock  = new();

    // Seed with 3 demo patients so Swagger is useful from the start
    public InMemoryPatientRepository()
    {
        _store.AddRange(new[]
        {
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
                CreatedAt   = DateTime.UtcNow.AddDays(-10),
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
                CreatedAt   = DateTime.UtcNow.AddDays(-5),
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
                CreatedAt   = DateTime.UtcNow.AddDays(-1),
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
                CreatedAt   = DateTime.UtcNow.AddDays(-1),
                CreatedBy   = "seed"
            }
        });
    }

    public Task<Patient?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var patient = _store.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
            return Task.FromResult(patient);
        }
    }

    public Task<Patient?> GetByMrnAsync(string mrn)
    {
        lock (_lock)
        {
            var patient = _store.FirstOrDefault(p =>
                p.MRN.Equals(mrn, StringComparison.OrdinalIgnoreCase) && !p.IsDeleted);
            return Task.FromResult(patient);
        }
    }

    public Task<IEnumerable<Patient>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        lock (_lock)
        {
            var result = _store
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult<IEnumerable<Patient>>(result);
        }
    }

    public Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        lock (_lock)
        {
            var term = searchTerm.ToLower();
            var result = _store
                .Where(p => !p.IsDeleted && (
                    p.FullName.ToLower().Contains(term) ||
                    p.Phone.Contains(term)              ||
                    p.MRN.ToLower().Contains(term)      ||
                    (p.Email != null && p.Email.ToLower().Contains(term))))
                .OrderBy(p => p.FullName)
                .ToList();
            return Task.FromResult<IEnumerable<Patient>>(result);
        }
    }

    public Task<Patient> AddAsync(Patient patient)
    {
        lock (_lock)
        {
            // Auto-generate MRN: MRN-XXXX
            var nextNum   = _store.Count + 1;
            patient.MRN   = $"MRN-{nextNum:D4}";
            patient.Id    = patient.Id == Guid.Empty ? Guid.NewGuid() : patient.Id;
            _store.Add(patient);
            return Task.FromResult(patient);
        }
    }

    public Task<Patient> UpdateAsync(Patient patient)
    {
        lock (_lock)
        {
            var index = _store.FindIndex(p => p.Id == patient.Id);
            if (index == -1)
                throw new KeyNotFoundException($"Patient {patient.Id} not found");

            _store[index]           = patient;
            _store[index].UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(_store[index]);
        }
    }

    public Task SoftDeleteAsync(Guid id)
    {
        lock (_lock)
        {
            var patient = _store.FirstOrDefault(p => p.Id == id);
            if (patient != null)
            {
                patient.IsDeleted = true;
                patient.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Any(p => p.Id == id && !p.IsDeleted));
        }
    }

    public Task<int> CountAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Count(p => !p.IsDeleted));
        }
    }
}
