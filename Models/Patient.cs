namespace PatientService.Models;

public class Patient
{
    public Guid   Id          { get; set; } = Guid.NewGuid();

    /// <summary>Medical Record Number — unique identifier used in hospital</summary>
    public string MRN         { get; set; } = string.Empty;

    public string FullName    { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string BloodGroup  { get; set; } = string.Empty;
    public string Phone       { get; set; } = string.Empty;
    public string? Email      { get; set; }
    public string? Address    { get; set; }

    // Audit columns — Week 2 onwards these will be auto-populated
    public bool      IsDeleted  { get; set; } = false;   // Soft delete — never hard-delete medical records
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public string    CreatedBy  { get; set; } = "system";
    public DateTime? UpdatedAt  { get; set; }
    public string?   UpdatedBy  { get; set; }
}
