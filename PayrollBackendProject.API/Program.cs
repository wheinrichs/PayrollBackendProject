using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using PayrollBackendProject.API.Utilities;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Service;
using PayrollBackendProject.Infrastructure.BackgroundJobs;
using PayrollBackendProject.Infrastructure.Auth;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.Infrastructure.Repository;
using System.Text;
using PayrollBackendProject.Infrastructure.Utilities;
using PayrollBackendProject.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add a health check to the service to monitor the status of the application
builder.Services.AddHealthChecks();

builder.Services.AddControllers();

builder.Configuration.AddEnvironmentVariables();

// Setup authentication and JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"] ?? throw new Exception("JWT key missing from configuraiton");
// Setup how the tokens should be processed and what should be evaluated within the token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // When checking the token, check the token issuer against the application issuer
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],

            // Checks the expiration of the token
            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    }
    );

// Enable authorization and dd a custom policy to restrict endpoints to approved clinicians
builder.Services.AddAuthorization( options =>
{
    options.AddPolicy("ApprovedClinicianOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleEnum.CLINICIAN.ToString());
        policy.RequireClaim("status", UserAccountApprovalStateEnum.APPROVED.ToString());
    });
    options.AddPolicy("ApprovedBackendOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleEnum.BACKEND.ToString(), RoleEnum.ADMIN.ToString());
        policy.RequireClaim("status", UserAccountApprovalStateEnum.APPROVED.ToString());
    });
    options.AddPolicy("ApprovedAdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(RoleEnum.ADMIN.ToString());
        policy.RequireClaim("status", UserAccountApprovalStateEnum.APPROVED.ToString());
    });
});

// Don't configure hangfire in testing environemnt
if (!builder.Environment.IsEnvironment("Testing"))
{
    // Add database information for Hangfire
    builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

    // Setup the hangfire background worker
    builder.Services.AddHangfireServer();
}

// Add service for clinician services
builder.Services.AddScoped<IClinicianService, ClinicianService>();
builder.Services.AddScoped<IClinicianRepository, ClinicianRepository>();

// Add service for user accounts services
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Add service for UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add service for EHR User accounts
builder.Services.AddScoped<IEHRUserAccountRepository, EHRUserAccountRepository>();

// Add service for CSV parsing
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IImportJob, ImportJob>();
builder.Services.AddScoped<ICsvParserService, CsvParserService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
builder.Services.AddScoped<IFingerprintGenerator, FingerprintGenerator>();
builder.Services.AddScoped<IFileHandler, FileHandler>();

// Add services for PayRun and PayStatements
builder.Services.AddScoped<IPayRunRepository, PayRunRepository>();
builder.Services.AddScoped<IPayStatementRepository, PayStatementRepository>();
builder.Services.AddScoped<IPayRunService, PayRunService>();
builder.Services.AddScoped<PayrollCalculator, PayrollCalculator>();

// Add service for tokens
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// Add services for logging
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Add database context, this specifies the type of database to connect to and the connection details
builder.Services.AddDbContext<ClinicianDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add health check to the db context. Registers a built in health check class internally
builder.Services.AddHealthChecks().AddDbContextCheck<ClinicianDbContext>(
    // This is the name of the health check that will be logged - can be anything
    name: "PayrollDBHealthCheck",
    // This is the status that gets reported if the health check fails. Defaults to unhealthy so redundant here
    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
    // This specifies the health check as a readiness check and not a liveliness check
    // Corresponds to the specified tag below under MapHealthChecks
    tags: new string[] { "Readiness check" });

// Add swagger support with documentation in the swagger doc and UI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PayrollBackendApi",
        Version = "v1",
        Description = "Backend API for Payroll"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    // Include XML docs from all the different projects
    var basePath = AppContext.BaseDirectory;

    var xmlFiles = new[]
    {
        "PayrollBackendProject.API.xml",
        "PayrollBackendProject.Application.xml"
    };

    foreach (var file in xmlFiles)
    {
        var path = Path.Combine(basePath, file);
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path);
        }
    }

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Always expose JSON (for CI / ReDoc)
app.UseSwagger();

// Only expose UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// Setup the hangfire dashboard 
// TODO FIX THIS AUTH
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] {new AllowAllDashboardAuthorizationFilter()}
    });
}

// Specify the endpoints for the health checks
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // This specifies that the endpoint should not run any custom checks. It should return 200 as long as it can respond
    // This is specifically for a liveliness check
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // This uses the method CanConnectAsync which is auto generated to see if it can connect to the DB
    // This predicate filters the health checks and only runs the tagged ones. 
    Predicate = check => check.Tags.Contains("Readiness check")
});

if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("Production"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Explicitly define the port so it works on render
// If the port is not injected accept any host on port 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

// Apply migrations to database by default
using (var scope = app.Services.CreateScope())
{
    
    var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();

public partial class Program { }
