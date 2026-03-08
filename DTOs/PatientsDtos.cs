namespace PatientService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────

public record RegisterPatientRequest(
    string FullName,
    DateTime DateOfBirth,
    string BloodGroup,   // A+, A-, B+, B-, AB+, AB-, O+, O-
    string Phone,
    string? Email,
    string? Address
);

public record UpdatePatientRequest(
    string FullName,
    string Phone,
    string? Email,
    string? Address
);

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────

public record PatientResponse(
    Guid Id,
    string MRN,
    string FullName,
    DateTime DateOfBirth,
    int AgeYears,
    string BloodGroup,
    string Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt
);

public record PatientListResponse(
    int Total,
    IEnumerable<PatientResponse> Patients
);

// ── API RESPONSE WRAPPER ─────────────────────────────────────────────────────

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    string? Error = null
)
{
    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new(true, message, data);

    public static ApiResponse<T> Fail(string error)
        => new(false, "Failed", default, error);
}
