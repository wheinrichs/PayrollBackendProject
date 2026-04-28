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

namespace PayrollBackendProject.IntegrationTests;

public class PayRunIntegrationTetsts : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PayRunIntegrationTetsts(CustomWebApplicationFactory factory)
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    private async Task<LoginResponseDTO> Signup(string role)
    {
        var email = $"user_{Guid.NewGuid()}@test.com";

        var res = await _client.PostAsJsonAsync("/api/auth/signup",
            new SignUpRequestDTO(email, "Password123!", "A", "B", role));

        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<LoginResponseDTO>())!;
    }

    private PaymentLineItem CreatePayment(
        Clinician? clinician,
        ImportBatch batch,
        EHRUser ehrUser,
        decimal amount,
        DateTime dos,
        DateTime appliedDate,
        int row)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            rawData: "raw",
            clinician: clinician,
            rawClinicianName: clinician != null ? $"{clinician.FirstName} {clinician.LastName}" : "Unknown",
            paymentAmount: amount,
            adjustmentAmount: 0,
            adjustmentCode: PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT,
            dateOfService: dos,
            patientId: "p1",
            cptCode: "90837",
            paymentId: Guid.NewGuid().ToString(),
            payer: "CIGNA",
            appliedBy: ehrUser,
            importBatch: batch,
            rowNumber: row,
            fingerprint: Guid.NewGuid().ToString(),
            appliedDate: appliedDate,
            paymentDate: appliedDate
        );
    }

    private async Task<ClinicianDbContext> ResetDb()
    {
        var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
        return db;
    }

    /*
    Test that generating a pay run only retrieves the payments in the given range and creates the correct statements.
    Test that the statements are grouped for the right clinicians and the totals are correct.
    */
    [Fact]
    public async Task GeneratePayRun_CorrectGrouping_AndDateFiltering()
    {
        var admin = await Signup("admin");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var db = await ResetDb();

        var clinician1 = new Clinician("A", "One", "c1@test.com", true, 0.6);
        var clinician2 = new Clinician("B", "Two", "c2@test.com", true, 0.6);

        var batch = new ImportBatch("file.csv", "fingerprint");
        var ehrUser = new EHRUser("A", "B", "AB");

        db.Clinicians.AddRange(clinician1, clinician2);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        var inRange = new DateTime(2026, 3, 1);
        var outRange = new DateTime(2025, 1, 1);
        var randomDOS = new DateTime(2024, 1, 1);

        db.PaymentLineItems.AddRange(
            CreatePayment(clinician1, batch, ehrUser, 100, randomDOS, inRange, 1),
            CreatePayment(clinician1, batch, ehrUser, 50,  randomDOS, inRange, 2),
            CreatePayment(clinician2, batch, ehrUser, 200, randomDOS, inRange, 3),
            CreatePayment(clinician2, batch, ehrUser, 999, randomDOS, outRange, 4)
        );

        await db.SaveChangesAsync();

        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 31);

        var res = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = start, EndDate = end });

        res.EnsureSuccessStatusCode();

        var payRunResponse = await res.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        var payRun = await db.PayRuns
            .Include(p => p.Statements)
            .ThenInclude(s => s.LineItems)
            .FirstAsync(p => p.Id == payRunResponse!.Id);

        Assert.Equal(2, payRun.Statements.Count);

        var c1 = payRun.Statements.First(s => s.ClinicianId == clinician1.ID);
        var c2 = payRun.Statements.First(s => s.ClinicianId == clinician2.ID);

        Assert.Equal(150, c1.TotalPayment);
        Assert.Equal(200, c2.TotalPayment);
    }

    /*
    Cannot generate a pay run without a valid token
    */
    [Fact]
    public async Task GeneratePayRun_FailsWithoutToken()
    {
        var res = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    /*
    Test that approving a pay run updates its status and that approving a pay statement updates its status. 
    */
    [Fact]
    public async Task ApprovePayRun_AndStatement_UpdatesStatus()
    {
        var admin = await Signup("admin");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var db = await ResetDb();

        var clinician = new Clinician("A", "One", "c@test.com", true, 0.6);
        var batch = new ImportBatch("file.csv", "fp");
        var ehrUser = new EHRUser("A", "B", "AB");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        var applied = DateTime.UtcNow.AddDays(-1);

        db.PaymentLineItems.Add(
            CreatePayment(clinician, batch, ehrUser, 100, DateTime.UtcNow, applied, 1)
        );

        await db.SaveChangesAsync();

        var gen = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = applied.AddDays(-1), EndDate = applied.AddDays(1) });

        var payRunResponse = await gen.Content.ReadFromJsonAsync<PayRunResponseDTO>();
        var payRunId = payRunResponse!.Id;

        var payRun = await db.PayRuns.Include(p => p.Statements).FirstAsync(p => p.Id == payRunId);
        var statementId = payRun.Statements.First().Id;

        await _client.PostAsync($"/approveRun/{payRunId}/approve", null);
        await _client.PostAsync($"/approveStatement/{statementId}/approve", null);

        using (var scope = _factory.Services.CreateScope())
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

            payRun = await freshDb.PayRuns
                .Include(p => p.Statements)
                .FirstAsync(p => p.Id == payRunId);
        }

        Assert.Equal(PayRunStatusEnum.COMPLETED, payRun.GenerationStatus);
        Assert.Equal(ApprovalStateEnum.APPROVED, payRun.Statements.First().ApprovalState);
    }

    /*
    Test that modifying a payment line item after its been added to a statement does not modify the statement
    */
    [Fact]
    public async Task Snapshot_IsImmutable_AfterModification()
    {
        var admin = await Signup("admin");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var db = await ResetDb();

        var clinician = new Clinician("A", "One", "c@test.com", true, 0.6);
        var batch = new ImportBatch("file.csv", "fp");
        var ehrUser = new EHRUser("A", "B", "AB");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        var applied = DateTime.UtcNow.AddDays(-1);

        var payment = CreatePayment(clinician, batch, ehrUser, 100, DateTime.UtcNow, applied, 1);
        db.PaymentLineItems.Add(payment);

        await db.SaveChangesAsync();

        await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = applied.AddDays(-1), EndDate = applied.AddDays(1) });

        var snapshot = await db.PaymentSnapshots.FirstAsync();

        payment.UpdateClinician(clinician);
        await db.SaveChangesAsync();

        var updatedSnapshot = await db.PaymentSnapshots.FirstAsync();

        Assert.Equal(100, updatedSnapshot.PaymentAmount);
    }

    /*
    Test that when a clinician is logged in and they request pay statements it only returns their pay statement. 
    */
   [Fact]
    public async Task Clinician_OnlyGetsTheirOwnStatements_RealFlow()
    {
        //  Sign up a clinician for the statement
        var signupResponse = await _client.PostAsJsonAsync("/api/auth/signup",
            new SignUpRequestDTO(
                email: "c1@test.com",
                password: "Password123!",
                firstName: "A",
                lastName: "One",
                role: "clinician"
            ));

        signupResponse.EnsureSuccessStatusCode();
        var clinicianAuth = await signupResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        // Get the clinician from the database
        Guid clinicianId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

            clinicianId = await db.Clinicians
                .Where(c => c.Email == "c1@test.com")
                .Select(c => c.ID)
                .FirstAsync();
        }

        // Set up payment data
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

            var clinician = await db.Clinicians.FirstAsync(c => c.ID == clinicianId);
            var otherClinician = new Clinician("B", "Two", "c2@test.com", true, 0.6);

            var batch = new ImportBatch("file.csv", "fp");
            var ehrUser = new EHRUser("A", "B", "AB");

            db.Clinicians.Add(otherClinician);
            db.ImportBatches.Add(batch);
            db.EHRUsers.Add(ehrUser);

            var applied = DateTime.UtcNow.AddDays(-1);

            db.PaymentLineItems.AddRange(
                CreatePayment(clinician, batch, ehrUser, 100, DateTime.UtcNow, applied, 1),
                CreatePayment(otherClinician, batch, ehrUser, 200, DateTime.UtcNow, applied, 2)
            );

            await db.SaveChangesAsync();
        }

        // Sign in as an admin and execute the payrun
        var admin = await Signup("admin");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var appliedDate = DateTime.UtcNow.AddDays(-1);

        var res = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = appliedDate.AddDays(-1), EndDate = appliedDate.AddDays(1) });

        res.EnsureSuccessStatusCode();

        var payRunResponse = await res.Content.ReadFromJsonAsync<PayRunResponseDTO>();
        var payRunId = payRunResponse!.Id;

        // Load the pay run and retrieve the statement ID for the new clinician 
        Guid clinicianStatementId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

            var payRun = await db.PayRuns
                .Include(p => p.Statements)
                .FirstAsync(p => p.Id == payRunId);

            clinicianStatementId = payRun.Statements
                .First(s => s.ClinicianId == clinicianId)
                .Id;
        }

        // Approve the statement for this clinician
        await _client.PostAsync($"/approveRun/{payRunId}/approve", null);
        await _client.PostAsync($"/approveStatement/{clinicianStatementId}/approve", null);

        // Sign in as the clinician
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", clinicianAuth!.Token);

        // Test the endpoint
        var statementsRes = await _client.GetAsync("/api/me/statements");
        statementsRes.EnsureSuccessStatusCode();

        var statements = await statementsRes.Content.ReadFromJsonAsync<List<PayStatementDTO>>();

        Assert.NotNull(statements);
        Assert.Single(statements);
        Console.WriteLine($"The clinician on the statement is {statements.First().Clinician.ID}");
    }
}
