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
using PayrollBackendProject.Infrastructure.Auth;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.IntegrationTests;

public class BusinessPayReportIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BusinessPayReportIntegrationTests(CustomWebApplicationFactory factory)
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    private async Task<LoginResponseDTO> Signup(string role, ClinicianDbContext db)
    {
        var email = $"user_{Guid.NewGuid()}@test.com";

        var passwordHasher = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IPasswordHasher>();

        var hashedPassword = passwordHasher.Hash("password1");
        var superAdmin = new UserAccount(email, hashedPassword, "X", "Y");
        superAdmin.Role = RoleEnum.ADMIN;
        superAdmin.UpdateUserAccountStatus(UserAccountApprovalStateEnum.APPROVED);
        db.Add<UserAccount>(superAdmin);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDTO(email, "password1"));
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

    private PaymentLineItem CreateCode500Payment(
        Clinician clinician,
        ImportBatch batch,
        EHRUser ehrUser,
        decimal adjustmentAmount,
        DateTime dos,
        DateTime appliedDate,
        int row)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            rawData: "raw-500",
            clinician: clinician,
            rawClinicianName: $"{clinician.FirstName} {clinician.LastName}",
            paymentAmount: 0m,
            adjustmentAmount: adjustmentAmount,
            adjustmentCode: PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK,
            dateOfService: dos,
            patientId: Guid.NewGuid().ToString(),
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
    Validate that a business pay report over the same range as a real pay run produces matching
    totals, and that generating the report does not persist any pay run or pay statement
    */
    [Fact]
    public async Task GenerateReport_MatchesRealPayRun_AndDoesNotPersistAnything()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("R1", "Test", $"r1_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Isolated historical range so this test's dates don't collide with other tests' pay runs
        var dos = new DateTime(2019, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2019, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        db.PaymentLineItems.Add(CreatePayment(clinician, batch, ehrUser, 1000m, dos, inRange, 1));
        await db.SaveChangesAsync();

        var start = new DateTime(2019, 3, 1);
        var end = new DateTime(2019, 3, 31);

        var payRunCountBefore = await db.PayRuns.CountAsync();
        var statementCountBefore = await db.PayStatements.CountAsync();

        var reportRes = await _client.PostAsJsonAsync("/api/businesspayreport", new { StartDate = start, EndDate = end });
        reportRes.EnsureSuccessStatusCode();
        var report = await reportRes.Content.ReadFromJsonAsync<BusinessPayReportResponseDTO>();

        var payRunCountAfter = await db.PayRuns.CountAsync();
        var statementCountAfter = await db.PayStatements.CountAsync();

        Assert.Equal(payRunCountBefore, payRunCountAfter);
        Assert.Equal(statementCountBefore, statementCountAfter);

        Assert.NotNull(report);
        Assert.Equal(1000m, report.GrossPaymentTotal);
        Assert.Equal(600m, report.StatementTotals);
        Assert.Equal(600m, report.TotalPayout);

        var entry = Assert.Single(report.ClinicianBreakdown);
        Assert.Equal(clinician.ID, entry.ClinicianId);
        Assert.Equal(600m, entry.CostShareAdjustedPayment);

        // Now actually generate a real pay run over the same range and confirm the totals match
        var payRunRes = await _client.PostAsJsonAsync("/api/payrun", new { StartDate = start, EndDate = end });
        payRunRes.EnsureSuccessStatusCode();
        var payRun = await payRunRes.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.Equal(payRun!.GrossPaymentTotal, report.GrossPaymentTotal);
        Assert.Equal(payRun.StatementTotals, report.StatementTotals);
        Assert.Equal(payRun.TotalPayout, report.TotalPayout);
    }

    /*
    Validate that the report reflects code-500 deductions already applied via a real, completed
    pay run whose applied date falls within the report's range
    */
    [Fact]
    public async Task GenerateReport_ReflectsHistoricalCode500Deductions()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("R2", "Test", $"r2_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Isolated historical range so this test's dates don't collide with other tests' pay runs
        var dos = new DateTime(2018, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2018, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        var normalPayment = CreatePayment(clinician, batch, ehrUser, 1000m, dos, inRange, 1);
        var takeback = CreateCode500Payment(clinician, batch, ehrUser, -200m, dos, inRange, 2);

        db.PaymentLineItems.AddRange(normalPayment, takeback);
        await db.SaveChangesAsync();

        var start = new DateTime(2018, 4, 1);
        var end = new DateTime(2018, 4, 30);

        // Actually run and apply the code 500 via a real pay run, so a Code500Application is persisted
        var payRunRes = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = start,
            EndDate = end,
            Code500Applications = new[] { new { PaymentLineItemId = takeback.Id, Amount = 200m } }
        });
        payRunRes.EnsureSuccessStatusCode();

        var reportRes = await _client.PostAsJsonAsync("/api/businesspayreport", new { StartDate = start, EndDate = end });
        reportRes.EnsureSuccessStatusCode();
        var report = await reportRes.Content.ReadFromJsonAsync<BusinessPayReportResponseDTO>();

        Assert.NotNull(report);
        Assert.Equal(200m, report.TotalCode500Deductions);
        Assert.Equal(1, report.TotalCode500LineItemCount);

        var entry = report.ClinicianBreakdown.Single(e => e.ClinicianId == clinician.ID);
        Assert.Equal(200m, entry.Code500Deductions);
        Assert.Equal(1, entry.Code500LineItemCount);
    }

    /*
    Validate that reports can be generated repeatedly over the same or overlapping date range -
    unlike pay runs, there is no overlap restriction on reports
    */
    [Fact]
    public async Task GenerateReport_AllowsRepeatedOverlappingRanges()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("R3", "Test", $"r3_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Isolated historical range so this test's dates don't collide with other tests' pay runs
        var dos = new DateTime(2017, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2017, 5, 15, 0, 0, 0, DateTimeKind.Utc);

        db.PaymentLineItems.Add(CreatePayment(clinician, batch, ehrUser, 500m, dos, inRange, 1));
        await db.SaveChangesAsync();

        var start = new DateTime(2017, 5, 1);
        var end = new DateTime(2017, 5, 31);

        var first = await _client.PostAsJsonAsync("/api/businesspayreport", new { StartDate = start, EndDate = end });
        var second = await _client.PostAsJsonAsync("/api/businesspayreport", new { StartDate = start, EndDate = end });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    /*
    Cannot generate a business pay report without a valid token
    */
    [Fact]
    public async Task GenerateReport_FailsWithoutToken()
    {
        var res = await _client.PostAsJsonAsync("/api/businesspayreport",
            new { StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
