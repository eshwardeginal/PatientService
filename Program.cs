using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Middleware;
using PatientService.Repositories;
using PatientService.Services;

// ─────────────────────────────────────────────────────────────────────────────
//  MediCloud — PatientService
//  Week 1: In-memory repository, deployed to Azure App Service
//  Week 2: Swap InMemoryPatientRepository → SqlPatientRepository (Azure SQL)
//  Week 3: Add BlobStorageService for patient documents
//  Week 6: Add Key Vault configuration provider
//  Week 7: Add Application Insights telemetry
//  Week 13: Add Azure AD B2C JWT Bearer authentication
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── SERVICES ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddHealthChecks(); // ✅ ADD THIS
builder.Services.AddEndpointsApiExplorer();

// Swagger — see all your APIs in a browser UI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "MediCloud — Patient Service",
        Version = "v1",
        Description = "Hospital Management System · Patient API · Built with .NET 8 + Azure"
    });
    // Week 13: add JWT auth to Swagger here
});

// ── REPOSITORY (Dependency Inversion) ────────────────────────────────────────
//
// WEEK 1:  Use in-memory (no DB needed)
//builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
//
// WEEK 2:  Comment above, uncomment below after adding EF Core + Azure SQL:
builder.Services.AddDbContext<PatientService.Data.MediCloudDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration["ConnectionStrings:AzureSQL"]));
builder.Services.AddScoped<IPatientRepository, SqlPatientRepository>();
//
// WEEK 6:  Connection string comes from Azure Key Vault — no changes needed here.
//          Just set App Service config to point to Key Vault reference.

// ── BUSINESS LOGIC ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPatientService, PatientServiceImpl>();

// ── CORS (allow Angular frontend later) ──────────────────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("MediCloudPolicy", policy =>
    policy.AllowAnyOrigin()      // Week 13: restrict to your Angular app URL
          .AllowAnyMethod()
          .AllowAnyHeader()));

// ── LOGGING ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Week 7: builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:InstrumentationKey"]);

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────
// ── ★ WEEK 2: Auto-run migrations on startup ──────────────────────────────────
// Creates the Patients table if it doesn't exist — no manual SQL scripts needed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MediCloudDbContext>();
    //await db.Database.MigrateAsync();               // runs pending migrations + seeds data
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
// ── MIDDLEWARE PIPELINE (order matters!) ──────────────────────────────────────
app.UseGlobalExceptionHandler();   // 1. Catch all exceptions first

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Staging"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediCloud Patient API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL — http://localhost:5000
    });
}

app.UseHttpsRedirection();
app.UseCors("MediCloudPolicy");
// ── ★ WEEK 2: Rich health check endpoint ─────────────────────────────────────
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = report.Status.ToString(),
            Service = "MediCloud PatientService",
            Version = "2.0.0",
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                // Week 8: add Redis here. Week 9: add Service Bus here.
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Week 13: app.UseAuthentication(); app.UseAuthorization();

app.MapControllers();

app.Run();
