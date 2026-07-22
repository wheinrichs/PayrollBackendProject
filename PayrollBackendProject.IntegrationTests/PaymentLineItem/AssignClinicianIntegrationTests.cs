using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.IntegrationTests;

public class AssignClinicianIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AssignClinicianIntegrationTests(CustomWebApplicationFactory factory)
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

    private PaymentLineItem CreateUnresolvedPayment(ImportBatch batch, EHRUser ehrUser, string rawClinicianName = "!SELECT PROVIDER")
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            rawData: "raw",
            clinician: null,
            rawClinicianName: rawClinicianName,
            paymentAmount: 100m,
            adjustmentAmount: 0,
            adjustmentCode: PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT,
            dateOfService: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            patientId: $"P-{Guid.NewGuid()}",
            cptCode: "90837",
            paymentId: $"PAY-{Guid.NewGuid()}",
            payer: "CIGNA",
            appliedBy: ehrUser,
            importBatch: batch,
            rowNumber: 1,
            fingerprint: Guid.NewGuid().ToString(),
            appliedDate: DateTime.UtcNow,
            paymentDate: DateTime.UtcNow
        );
    }

    /*
    Validate that POST /api/payments/{id}/assign-clinician returns 204 and updates the payment's
    clinician when given valid IDs
    */
    [Fact]
    public async Task AssignClinician_WithValidIds_Returns204AndUpdatesRecord()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");
        var clinician = new Clinician("Jane", "Smith", $"jane_{Guid.NewGuid()}@clinic.com");
        var payment = CreateUnresolvedPayment(batch, ehrUser);

        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);
        db.Clinicians.Add(clinician);
        db.PaymentLineItems.Add(payment);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/assign-clinician", new { ClinicianId = clinician.ID });

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var updated = await GetDb().PaymentLineItems.FirstAsync(p => p.Id == payment.Id);
        Assert.Equal(clinician.ID, updated.ClinicianId);
        Assert.Equal(PaymentLineItemStatusEnum.VALID, updated.PaymentLineItemStatus);
    }

    /*
    Validate that POST /api/payments/{id}/assign-clinician returns 404 when the payment does not exist
    */
    [Fact]
    public async Task AssignClinician_WithUnknownPaymentId_Returns404()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var clinician = new Clinician("Jane", "Smith", $"jane_{Guid.NewGuid()}@clinic.com");
        db.Clinicians.Add(clinician);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync($"/api/payments/{Guid.NewGuid()}/assign-clinician", new { ClinicianId = clinician.ID });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /*
    Validate that POST /api/payments/{id}/assign-clinician returns 404 when the clinician does not exist
    */
    [Fact]
    public async Task AssignClinician_WithUnknownClinicianId_Returns404()
    {
        var db = GetDb();
        var login = await Signup(db);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");
        var payment = CreateUnresolvedPayment(batch, ehrUser);

        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);
        db.PaymentLineItems.Add(payment);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/assign-clinician", new { ClinicianId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /*
    Validate that POST /api/payments/{id}/assign-clinician returns 401 when no authorization token is provided
    */
    [Fact]
    public async Task AssignClinician_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.PostAsJsonAsync($"/api/payments/{Guid.NewGuid()}/assign-clinician", new { ClinicianId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
