using Microsoft.AspNetCore.Mvc;

namespace PatientService.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
// Week 7: Azure Application Insights Availability Tests will ping /health
// This endpoint tells Azure if the service is healthy.
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger) => _logger = logger;

    /// <summary>Health check — used by Azure App Insights availability tests</summary>
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check called at {Time}", DateTime.UtcNow);

        return Ok(new
        {
            Status = "Healthy",
            Service = "MediCloud PatientService",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            // Week 2: add DB connectivity check here
            // Week 7: this gets called by App Insights every 5 mins
        });
    }
}
