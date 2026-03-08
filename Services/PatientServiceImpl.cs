using PatientService.DTOs;
using PatientService.Models;
using PatientService.Repositories;

namespace PatientService.Services;

// ─────────────────────────────────────────────────────────────────────────────
// Single Responsibility Principle — this class ONLY handles patient business logic.
// It doesn't know about HTTP, SQL, or Azure — those are other layers' jobs.
// ─────────────────────────────────────────────────────────────────────────────

public interface IPatientService
{
    Task<PatientResponse>     RegisterAsync(RegisterPatientRequest request, string createdBy);
    Task<PatientResponse?>    GetByIdAsync(Guid id);
    Task<PatientResponse?>    GetByMrnAsync(string mrn);
    Task<PatientListResponse> GetAllAsync(int page, int pageSize);
    Task<PatientListResponse> SearchAsync(string searchTerm);
    Task<PatientResponse>     UpdateAsync(Guid id, UpdatePatientRequest request, string updatedBy);
    Task                      DeleteAsync(Guid id);
}

public class PatientServiceImpl : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly ILogger<PatientServiceImpl> _logger;

    // Dependency Inversion — depends on IPatientRepository, not InMemoryPatientRepository
    public PatientServiceImpl(IPatientRepository repo, ILogger<PatientServiceImpl> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<PatientResponse> RegisterAsync(RegisterPatientRequest request, string createdBy)
    {
        _logger.LogInformation("Registering new patient: {Name}", request.FullName);

        var patient = new Patient
        {
            FullName    = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            BloodGroup  = request.BloodGroup.ToUpper(),
            Phone       = request.Phone.Trim(),
            Email       = request.Email?.Trim().ToLower(),
            Address     = request.Address?.Trim(),
            CreatedBy   = createdBy,
            CreatedAt   = DateTime.UtcNow
        };

        var saved = await _repo.AddAsync(patient);

        _logger.LogInformation("Patient registered with MRN: {MRN}", saved.MRN);

        // Week 5: here you will publish PatientRegisteredEvent to Service Bus
        // await _publisher.PublishAsync(new PatientRegisteredEvent(saved.Id, saved.MRN));

        return MapToResponse(saved);
    }

    public async Task<PatientResponse?> GetByIdAsync(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);
        return patient is null ? null : MapToResponse(patient);
    }

    public async Task<PatientResponse?> GetByMrnAsync(string mrn)
    {
        var patient = await _repo.GetByMrnAsync(mrn);
        return patient is null ? null : MapToResponse(patient);
    }

    public async Task<PatientListResponse> GetAllAsync(int page, int pageSize)
    {
        var patients = await _repo.GetAllAsync(page, pageSize);
        var total    = await _repo.CountAsync();
        return new PatientListResponse(total, patients.Select(MapToResponse));
    }

    public async Task<PatientListResponse> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(1, 20);

        var patients = await _repo.SearchAsync(searchTerm.Trim());
        var list     = patients.ToList();
        return new PatientListResponse(list.Count, list.Select(MapToResponse));
    }

    public async Task<PatientResponse> UpdateAsync(Guid id, UpdatePatientRequest request, string updatedBy)
    {
        var patient = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Patient {id} not found");

        patient.FullName  = request.FullName.Trim();
        patient.Phone     = request.Phone.Trim();
        patient.Email     = request.Email?.Trim().ToLower();
        patient.Address   = request.Address?.Trim();
        patient.UpdatedAt = DateTime.UtcNow;
        patient.UpdatedBy = updatedBy;

        var updated = await _repo.UpdateAsync(patient);

        _logger.LogInformation("Patient {Id} updated by {User}", id, updatedBy);

        return MapToResponse(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        if (!await _repo.ExistsAsync(id))
            throw new KeyNotFoundException($"Patient {id} not found");

        await _repo.SoftDeleteAsync(id);

        _logger.LogInformation("Patient {Id} soft-deleted", id);
    }

    // ── Private mapper — keeps DTOs separate from domain model ─────────────

    private static PatientResponse MapToResponse(Patient p) => new(
        Id:          p.Id,
        MRN:         p.MRN,
        FullName:    p.FullName,
        DateOfBirth: p.DateOfBirth,
        AgeYears:    CalculateAge(p.DateOfBirth),
        BloodGroup:  p.BloodGroup,
        Phone:       p.Phone,
        Email:       p.Email,
        Address:     p.Address,
        CreatedAt:   p.CreatedAt
    );

    private static int CalculateAge(DateTime dob)
    {
        var today = DateTime.Today;
        var age   = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;
        return age;
    }
}
