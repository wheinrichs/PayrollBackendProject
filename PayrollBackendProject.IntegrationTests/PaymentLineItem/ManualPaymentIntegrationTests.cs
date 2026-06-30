using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.IntegrationTests;

public class ManualPaymentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ManualPaymentIntegrationTests(CustomWebApplicationFactory factory)
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    private async Task<LoginResponseDTO> Signup(ClinicianDbContext db)
    {
        var email = $"admin_{Guid.NewGuid()}@test.com";
        var passwordHasher = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hashedPassword = passwordHasher.Hash("password1");

        var admin = new UserAccount(email, hashedPassword, "Admin", "User");
        admin.Role = RoleEnum.ADMIN;
        admin.UpdateUserAccountStatus(UserAccountApprovalStateEnum.APPROVED);
        db.Add(admin);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO(email, "password1"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<LoginResponseDTO>())!;
    }

    private ClinicianDbContext GetDb()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
    }

    private ManualPaymentRequestDTO BuildValidRequest(Guid? clinicianId = null) => new()
    {
        PaymentAmount = 150m,
        AdjustmentAmount = 0m,
        PaymentAdjustmentCode = (int)PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT,
        DateOfService = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        PatientId = $"P-{Guid.NewGuid()}",
        CPTCode = "90837",
        PaymentId = $"PAY-{Guid.NewGuid()}",
        Payer = "CIGNA",
        RawClinicianName = "Jane Smith",
        ClinicianId = clinicianId,
        AppliedDate = DateTime.UtcNow,
        PaymentDate = DateTime.UtcNow
    };

    /*
    Validate that POST /api/payments/manual returns 401 when no authorization token is provided
    */
    [Fact]
    public async Task AddManualPayment_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.PostAsJsonAsync("/api/payments/manual", BuildValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    /*
    Validate that POST /api/payments/manual returns 201 and a valid Guid for a well-formed
    request that does not specify a clinician
    */
    [Fact]
    public async Task AddManualPayment_WithoutClinician_Returns201WithId()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var res = await _client.PostAsJsonAsync("/api/payments/manual", BuildValidRequest());

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var returnedId = await res.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, returnedId);
    }

    /*
    Validate that POST /api/payments/manual returns 201 and correctly associates the new payment
    with an existing clinician when a valid ClinicianId is provided
    */
    [Fact]
    public async Task AddManualPayment_WithValidClinician_Returns201()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var clinician = new Clinician("Jane", "Smith", $"jane_{Guid.NewGuid()}@clinic.com");
        db.Clinicians.Add(clinician);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync("/api/payments/manual", BuildValidRequest(clinicianId: clinician.ID));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var returnedId = await res.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, returnedId);
    }

    /*
    Validate that POST /api/payments/manual returns 409 Conflict when a payment with the same
    identifying details (same fingerprint) has already been submitted
    */
    [Fact]
    public async Task AddManualPayment_WithDuplicateDetails_Returns409()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var request = BuildValidRequest();

        var first = await _client.PostAsJsonAsync("/api/payments/manual", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/payments/manual", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    /*
    Validate that POST /api/payments/manual returns 400 Bad Request when the PaymentAdjustmentCode
    does not correspond to a defined enum value
    */
    [Fact]
    public async Task AddManualPayment_WithInvalidAdjustmentCode_Returns400()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var request = BuildValidRequest();
        request.PaymentAdjustmentCode = 9999;

        var res = await _client.PostAsJsonAsync("/api/payments/manual", request);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /*
    Validate that POST /api/payments/manual returns 404 Not Found when the provided ClinicianId
    does not match any clinician in the database
    */
    [Fact]
    public async Task AddManualPayment_WithNonExistentClinicianId_Returns404()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var res = await _client.PostAsJsonAsync("/api/payments/manual", BuildValidRequest(clinicianId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
