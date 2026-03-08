using Microsoft.AspNetCore.Mvc;
using PatientService.DTOs;
using PatientService.Services;

namespace PatientService.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
// Thin controller — only HTTP concerns here.
// All business logic lives in IPatientService.
// Controller doesn't know about the database or Azure at all.
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService service, ILogger<PatientsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── GET /api/patients?page=1&pageSize=20 ─────────────────────────────────
    /// <summary>Get all patients with pagination</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PatientListResponse>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PatientListResponse>.Ok(result,
            $"Retrieved {result.Patients.Count()} of {result.Total} patients"));
    }

    // ── GET /api/patients/search?term=ravi ───────────────────────────────────
    /// <summary>Search patients by name, phone, MRN, or email</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PatientListResponse>), 200)]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(ApiResponse<PatientListResponse>.Fail("Search term is required"));

        var result = await _service.SearchAsync(term);
        return Ok(ApiResponse<PatientListResponse>.Ok(result,
            $"Found {result.Total} patients matching '{term}'"));
    }

    // ── GET /api/patients/{id} ───────────────────────────────────────────────
    /// <summary>Get patient by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var patient = await _service.GetByIdAsync(id);
        if (patient is null)
            return NotFound(ApiResponse<PatientResponse>.Fail($"Patient {id} not found"));

        return Ok(ApiResponse<PatientResponse>.Ok(patient));
    }

    // ── GET /api/patients/mrn/{mrn} ──────────────────────────────────────────
    /// <summary>Get patient by Medical Record Number</summary>
    [HttpGet("mrn/{mrn}")]
    [ProducesResponseType(typeof(ApiResponse<PatientResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByMrn(string mrn)
    {
        var patient = await _service.GetByMrnAsync(mrn);
        if (patient is null)
            return NotFound(ApiResponse<PatientResponse>.Fail($"Patient with MRN '{mrn}' not found"));

        return Ok(ApiResponse<PatientResponse>.Ok(patient));
    }

    // ── POST /api/patients ───────────────────────────────────────────────────
    /// <summary>Register a new patient</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientResponse>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterPatientRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<PatientResponse>.Fail("Invalid request data"));

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(ApiResponse<PatientResponse>.Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(ApiResponse<PatientResponse>.Fail("Phone is required"));

        var validBloodGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        if (!validBloodGroups.Contains(request.BloodGroup?.ToUpper()))
            return BadRequest(ApiResponse<PatientResponse>.Fail(
                $"Invalid blood group. Must be one of: {string.Join(", ", validBloodGroups)}"));

        // Get caller identity — Week 13 this will come from Azure AD B2C JWT token
        var createdBy = User.Identity?.Name ?? "anonymous";

        var patient = await _service.RegisterAsync(request, createdBy);

        return CreatedAtAction(
            nameof(GetById),
            new { id = patient.Id },
            ApiResponse<PatientResponse>.Ok(patient, $"Patient registered with MRN: {patient.MRN}"));
    }

    // ── PUT /api/patients/{id} ───────────────────────────────────────────────
    /// <summary>Update patient details</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(ApiResponse<PatientResponse>.Fail("Full name is required"));

        var updatedBy = User.Identity?.Name ?? "anonymous";

        try
        {
            var patient = await _service.UpdateAsync(id, request, updatedBy);
            return Ok(ApiResponse<PatientResponse>.Ok(patient, "Patient updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PatientResponse>.Fail(ex.Message));
        }
    }

    // ── DELETE /api/patients/{id} ────────────────────────────────────────────
    /// <summary>Soft-delete a patient (record is kept, just hidden)</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok("Patient deleted", "Soft delete successful — record retained for audit"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
