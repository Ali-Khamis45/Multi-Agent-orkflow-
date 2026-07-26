using System.Text;
using System.Text.Json.Serialization;
using AiAgentsTeam.Api;
using AiAgentsTeam.Api.EventRelay;
using AiAgentsTeam.Api.Hubs;
using AiAgentsTeam.Api.Middleware;
using AiAgentsTeam.Application;
using AiAgentsTeam.Application.Scheduling;
using AiAgentsTeam.Infrastructure;
using AiAgentsTeam.Infrastructure.Auth;
using AiAgentsTeam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Every enum in a request/response body (ArtifactType, MemoryLayer, MemoryKind,
// SupervisorDecisionType, ReasoningStage, ...) is exchanged as its string name —
// matching the AI Runtime's Pydantic models and this API's own EF Core string
// conversions (§ ArtifactConfiguration etc.) — never as a raw integer.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// Phase 4 Connector Framework encrypts every stored credential (OAuth token, API key)
// via Data Protection — without an explicit persistent key ring, ASP.NET Core generates
// one in-memory/ephemeral per process, so credentials encrypted before a restart become
// permanently undecryptable after one (confirmed live: a container recreate mid-session
// broke every previously-installed connector with a CryptographicException). The
// DataProtection-Keys volume in docker-compose.yml is what this path needs to survive
// restarts; a single, unnamed key ring for the whole app (not per-connector) is correct
// since Data Protection's protector *purpose string* ("AiAgentsTeam.ConnectorCredentials.v1",
// see CredentialProtector) already provides per-purpose key separation.
builder.Services.AddDataProtection()
    .SetApplicationName("AiAgentsTeam")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"] ?? "/tmp/dataprotection-keys"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SignalRRelayHostedService>();

// Auth (Phase 2, "AI Enterprise OS") — the platform's first authentication layer;
// see docs/reviews/SECURITY_REVIEW.md for what was true before this existed.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
builder.Services.Configure<InternalServiceOptions>(builder.Configuration.GetSection(InternalServiceOptions.SectionName));

// Configuration Layer (Phase 1.5 §9): retry policy is an operational tuning knob,
// bound from appsettings/env per environment rather than a hardcoded constant.
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Phase 1 convenience: apply migrations automatically on startup in dev so
    // `docker compose up` produces a working schema with no manual step.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WorkflowHub>("/hubs/workflow");

app.Run();
