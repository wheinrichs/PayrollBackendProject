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

    private async Task<LoginResponseDTO> Signup(string role, ClinicianDbContext db)
    {
        // Create an already approved super admin
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
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

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
        var db = await ResetDb();
        
        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

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
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("A", "One", "c@test.com", true, 0.6);
        var batch = new ImportBatch("file.csv", "fp");
        var ehrUser = new EHRUser("A", "B", "AB");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use a date window offset from the other relative-date tests in this file so their
        // pay run ranges don't overlap (pay run generation now rejects overlapping date ranges)
        var applied = DateTime.UtcNow.AddDays(-11);

        var payment = CreatePayment(clinician, batch, ehrUser, 100, DateTime.UtcNow, applied, 1);
        db.PaymentLineItems.Add(payment);

        await db.SaveChangesAsync();

        await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = applied.AddDays(-1), EndDate = applied.AddDays(1) });

        // Filter by the specific payment to avoid returning snapshots created by other tests
        var snapshot = await db.PaymentSnapshots.FirstAsync(s => s.PaymentLineItemId == payment.Id);

        payment.UpdateClinician(clinician);
        await db.SaveChangesAsync();

        var updatedSnapshot = await db.PaymentSnapshots.FirstAsync(s => s.PaymentLineItemId == payment.Id);

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
        var scope = _factory.Services.CreateScope();
        
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

        clinicianId = await db.Clinicians
            .Where(c => c.Email == "c1@test.com")
            .Select(c => c.ID)
            .FirstAsync();


        var clinician = await db.Clinicians.FirstAsync(c => c.ID == clinicianId);
        var otherClinician = new Clinician("B", "Two", "c2@test.com", true, 0.6);

        var batch = new ImportBatch("file.csv", "fp");
        var ehrUser = new EHRUser("A", "B", "AB");

        db.Clinicians.Add(otherClinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use a date window offset from the other relative-date tests in this file so their
        // pay run ranges don't overlap (pay run generation now rejects overlapping date ranges)
        var applied = DateTime.UtcNow.AddDays(-21);

        db.PaymentLineItems.AddRange(
            CreatePayment(clinician, batch, ehrUser, 100, DateTime.UtcNow, applied, 1),
            CreatePayment(otherClinician, batch, ehrUser, 200, DateTime.UtcNow, applied, 2)
        );

        // Manually approve the clinician account and set the role
        var clinicianUser = await db.Users.FirstAsync(u => u.Email == "c1@test.com");
        clinicianUser.Role = RoleEnum.CLINICIAN;
        clinicianUser.UpdateUserAccountStatus(UserAccountApprovalStateEnum.APPROVED);

        await db.SaveChangesAsync();
        
        // Sign in as an admin and execute the payrun
        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var appliedDate = DateTime.UtcNow.AddDays(-21);

        var res = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = appliedDate.AddDays(-1), EndDate = appliedDate.AddDays(1) });

        res.EnsureSuccessStatusCode();

        var payRunResponse = await res.Content.ReadFromJsonAsync<PayRunResponseDTO>();
        var payRunId = payRunResponse!.Id;

        // Load the pay run and retrieve the statement ID for the new clinician 
        Guid clinicianStatementId;

        var payRun = await db.PayRuns
            .Include(p => p.Statements)
            .FirstAsync(p => p.Id == payRunId);

        clinicianStatementId = payRun.Statements
            .First(s => s.ClinicianId == clinicianId)
            .Id;
       

        // Approve the statement for this clinician
        await _client.PostAsync($"/approveRun/{payRunId}/approve", null);
        await _client.PostAsync($"/approveStatement/{clinicianStatementId}/approve", null);

        // Sign in as the clinician
        var clinicianLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDTO("c1@test.com", "Password123!")
        );

        var loginBody = await clinicianLoginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        // Test the endpoint
        var statementsRes = await _client.GetAsync("/api/me/statements");
        statementsRes.EnsureSuccessStatusCode();

        var statements = await statementsRes.Content.ReadFromJsonAsync<List<PayStatementDTO>>();

        Assert.NotNull(statements);
        Assert.Single(statements);
        Console.WriteLine($"The clinician on the statement is {statements.First().Clinician.ID}");
    }

    /*
    Validate that GET /api/payments/takebacks/pending returns only unapplied INSURANCE_TAKEBACK
    payments, not regular payments and not already-applied takebacks
    */
    [Fact]
    public async Task GetUnappliedCode500Payments_ReturnsOnlyPendingTakebacks()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C5", "Test", $"c5_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use a date range not shared by any pay run in other tests to avoid DB pollution
        var dos = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var applied = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        // One unapplied takeback (should appear in results)
        var unappliedTakeback = CreateCode500Payment(clinician, batch, ehrUser, -150m, dos, applied, 1);

        // One already-applied takeback (should NOT appear)
        var appliedTakeback = CreateCode500Payment(clinician, batch, ehrUser, -100m, dos, applied, 2);
        var dummyPayRun = PayRun.GeneratePayRun(applied.AddDays(-1), applied);
        appliedTakeback.ApplyCode500(100m, Guid.NewGuid(), dummyPayRun);

        // One regular payment (should NOT appear)
        var regularPayment = CreatePayment(clinician, batch, ehrUser, 500m, dos, applied, 3);

        db.PaymentLineItems.AddRange(unappliedTakeback, appliedTakeback, regularPayment);
        await db.SaveChangesAsync();

        var res = await _client.GetAsync("/api/payments/takebacks/pending");
        res.EnsureSuccessStatusCode();

        var results = await res.Content.ReadFromJsonAsync<List<UnappliedCode500ResponseDTO>>();

        Assert.NotNull(results);
        // Other tests may have seeded unapplied code 500s — assert the expected one is present
        // and the already-applied takeback is absent
        Assert.Contains(results, r => r.Id == unappliedTakeback.Id);
        Assert.DoesNotContain(results, r => r.Id == appliedTakeback.Id);
        Assert.DoesNotContain(results, r => r.Id == regularPayment.Id);
    }

    /*
    Validate that an unapplied code 500 payment is excluded from the pay run, so the statement
    total reflects only the normal payments and TotalCode500Deductions is zero
    */
    [Fact]
    public async Task GeneratePayRun_ExcludesUnappliedCode500_FromStatement()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C6", "Test", $"c6_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use May 2024 — an isolated month not used by any other test's pay run range
        var dos = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc);

        // Normal payment: $500
        db.PaymentLineItems.Add(CreatePayment(clinician, batch, ehrUser, 500m, dos, inRange, 1));

        // Unapplied code 500 takeback: -$200 adjustment (should be excluded from pay run)
        db.PaymentLineItems.Add(CreateCode500Payment(clinician, batch, ehrUser, -200m, dos, inRange, 2));

        await db.SaveChangesAsync();

        var start = new DateTime(2024, 5, 1);
        var end = new DateTime(2024, 5, 31);

        var res = await _client.PostAsJsonAsync("/api/payrun", new { StartDate = start, EndDate = end });
        res.EnsureSuccessStatusCode();

        var payRunResponse = await res.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.NotNull(payRunResponse);
        // The unapplied code 500 is excluded — TotalCode500Deductions should be zero
        Assert.Equal(0m, payRunResponse.TotalCode500Deductions);
        // Statement total = 500 * 0.6 = 300 (only the normal payment counts)
        Assert.Equal(300m, payRunResponse.StatementTotals);
    }

    /*
    Full end-to-end: request a code 500 application as part of generating a pay run and verify
    the statement's cost-share-adjusted amount is reduced and TotalCode500Deductions is correct
    */
    [Fact]
    public async Task GeneratePayRun_WithCode500Application_ReducesStatementAmount_AndReportsDeductions()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C7", "Test", $"c7_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use July 2024 — an isolated month not used by any other test's pay run range
        var dos = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);

        // Normal payment: $1000
        var normalPayment = CreatePayment(clinician, batch, ehrUser, 1000m, dos, inRange, 1);
        // Outstanding code 500 takeback: -$200
        var takeback = CreateCode500Payment(clinician, batch, ehrUser, -200m, dos, inRange, 2);

        db.PaymentLineItems.AddRange(normalPayment, takeback);
        await db.SaveChangesAsync();

        // Generate the pay run, requesting the full $200 balance be applied to this run
        var start = new DateTime(2024, 7, 1);
        var end = new DateTime(2024, 7, 31);

        var payRunRes = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = start,
            EndDate = end,
            Code500Applications = new[] { new { PaymentLineItemId = takeback.Id, Amount = 200m } }
        });
        payRunRes.EnsureSuccessStatusCode();

        var payRunResponse = await payRunRes.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.NotNull(payRunResponse);
        // Code 500 deduction = $200
        Assert.Equal(200m, payRunResponse.TotalCode500Deductions);
        // CostShareAdjustedPayment = (1000 - 200) * 0.6 = 480
        Assert.Equal(480m, payRunResponse.StatementTotals);
        // Gross total reflects the normal payment amount only (takeback has $0 paymentAmount)
        Assert.Equal(1000m, payRunResponse.GrossPaymentTotal);

        // Verify statement-level numbers in the DB
        var payRun = await db.PayRuns
            .Include(p => p.Statements)
            .FirstAsync(p => p.Id == payRunResponse.Id);

        var statement = payRun.Statements.Single();
        Assert.Equal(1000m, statement.TotalPayment);
        Assert.Equal(480m, statement.CostShareAdjustedPayment);

        using (var scope = _factory.Services.CreateScope())
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
            var updatedTakeback = await freshDb.PaymentLineItems.FirstAsync(p => p.Id == takeback.Id);
            Assert.True(updatedTakeback.IsCode500Applied);
            Assert.Equal(0m, updatedTakeback.RemainingCode500Amount);
        }
    }

    /*
    Validate that applying only part of an outstanding code 500 balance to a pay run deducts only
    that amount, leaves a remaining balance visible in the pending-takebacks queue, and that a
    later pay run can apply the remainder with no double count
    */
    [Fact]
    public async Task GeneratePayRun_WithPartialCode500Application_CarriesRemainderToFuturePayRun()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C10", "Test", $"c10_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use September and October 2024 — isolated months not used by any other test's pay run range
        var septDos = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var septInRange = new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var octDos = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var octInRange = new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        var septPayment = CreatePayment(clinician, batch, ehrUser, 1000m, septDos, septInRange, 1);
        var octPayment = CreatePayment(clinician, batch, ehrUser, 1000m, octDos, octInRange, 2);
        // Outstanding code 500 takeback: -$200, too large to fully take out of the September run
        var takeback = CreateCode500Payment(clinician, batch, ehrUser, -200m, septDos, septInRange, 3);

        db.PaymentLineItems.AddRange(septPayment, octPayment, takeback);
        await db.SaveChangesAsync();

        // First pay run: apply only $50 of the $200 balance
        var firstRes = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = new DateTime(2024, 9, 1),
            EndDate = new DateTime(2024, 9, 30),
            Code500Applications = new[] { new { PaymentLineItemId = takeback.Id, Amount = 50m } }
        });
        firstRes.EnsureSuccessStatusCode();
        var firstPayRun = await firstRes.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.Equal(50m, firstPayRun!.TotalCode500Deductions);

        // The remaining $150 balance is still visible in the pending-takebacks queue
        var pendingRes = await _client.GetAsync("/api/payments/takebacks/pending");
        pendingRes.EnsureSuccessStatusCode();
        var pending = await pendingRes.Content.ReadFromJsonAsync<List<UnappliedCode500ResponseDTO>>();
        var pendingEntry = pending!.Single(p => p.Id == takeback.Id);
        Assert.Equal(150m, pendingEntry.RemainingAmount);
        Assert.Equal(50m, pendingEntry.AppliedAmount);

        // Second pay run (October): apply the remaining $150
        var secondRes = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = new DateTime(2024, 10, 1),
            EndDate = new DateTime(2024, 10, 31),
            Code500Applications = new[] { new { PaymentLineItemId = takeback.Id, Amount = 150m } }
        });
        secondRes.EnsureSuccessStatusCode();
        var secondPayRun = await secondRes.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.Equal(150m, secondPayRun!.TotalCode500Deductions);

        // No double count: the two runs' deductions sum to the original $200 balance
        Assert.Equal(200m, firstPayRun.TotalCode500Deductions + secondPayRun.TotalCode500Deductions);

        // The line item is now fully applied and no longer in the pending queue
        var finalPendingRes = await _client.GetAsync("/api/payments/takebacks/pending");
        var finalPending = await finalPendingRes.Content.ReadFromJsonAsync<List<UnappliedCode500ResponseDTO>>();
        Assert.DoesNotContain(finalPending!, p => p.Id == takeback.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var freshDb = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
            var updatedTakeback = await freshDb.PaymentLineItems.FirstAsync(p => p.Id == takeback.Id);
            Assert.True(updatedTakeback.IsCode500Applied);
        }
    }

    /*
    Validate that requesting more than the remaining outstanding code 500 balance returns a 400
    and does not persist a pay run
    */
    [Fact]
    public async Task GeneratePayRun_WithCode500ApplicationExceedingBalance_ReturnsBadRequest()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C11", "Test", $"c11_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use November 2024 — an isolated month not used by any other test's pay run range
        var dos = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2024, 11, 15, 0, 0, 0, DateTimeKind.Utc);

        var takeback = CreateCode500Payment(clinician, batch, ehrUser, -200m, dos, inRange, 1);
        db.PaymentLineItems.Add(takeback);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = new DateTime(2024, 11, 1),
            EndDate = new DateTime(2024, 11, 30),
            Code500Applications = new[] { new { PaymentLineItemId = takeback.Id, Amount = 500m } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var payRunCount = await db.PayRuns.CountAsync(p => p.Statements.Any(s => s.ClinicianId == clinician.ID));
        Assert.Equal(0, payRunCount);
    }

    /*
    Validate that generating a pay run with IncludePsychTodayPayout enabled adds the flat payout
    only to eligible (HasPsychToday) clinicians, and reports it as a separate total on the response
    */
    [Fact]
    public async Task GeneratePayRun_WithPsychTodayPayoutEnabled_AddsPayoutForEligibleCliniciansOnly()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var eligibleClinician = new Clinician("C8", "Eligible", $"c8_{Guid.NewGuid()}@test.com", true, 0.6);
        var ineligibleClinician = new Clinician("C9", "Ineligible", $"c9_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.AddRange(eligibleClinician, ineligibleClinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Use August 2024 — an isolated month not used by any other test's pay run range
        var dos = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var inRange = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc);

        db.PaymentLineItems.AddRange(
            CreatePayment(eligibleClinician, batch, ehrUser, 500m, dos, inRange, 1),
            CreatePayment(ineligibleClinician, batch, ehrUser, 500m, dos, inRange, 2)
        );

        await db.SaveChangesAsync();

        var start = new DateTime(2024, 8, 1);
        var end = new DateTime(2024, 8, 31);

        var res = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = start,
            EndDate = end,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = 75m
        });
        res.EnsureSuccessStatusCode();

        var payRunResponse = await res.Content.ReadFromJsonAsync<PayRunResponseDTO>();

        Assert.NotNull(payRunResponse);
        // Only the one eligible clinician received the payout
        Assert.Equal(75m, payRunResponse.TotalPsychTodayPayout);
        Assert.Equal(payRunResponse.StatementTotals + 75m, payRunResponse.TotalPayout);

        var payRun = await db.PayRuns
            .Include(p => p.Statements)
            .FirstAsync(p => p.Id == payRunResponse.Id);

        var eligibleStatement = payRun.Statements.Single(s => s.ClinicianId == eligibleClinician.ID);
        var ineligibleStatement = payRun.Statements.Single(s => s.ClinicianId == ineligibleClinician.ID);

        Assert.Equal(75m, eligibleStatement.PsychTodayPayout);
        Assert.Equal(eligibleStatement.CostShareAdjustedPayment + 75m, eligibleStatement.TotalPayout);
        Assert.Equal(0m, ineligibleStatement.PsychTodayPayout);
        Assert.Equal(ineligibleStatement.CostShareAdjustedPayment, ineligibleStatement.TotalPayout);
    }

    /*
    Validate that a new pay run cannot start on or before the end date of an existing pay run -
    e.g. an existing run of 6/15-6/22 blocks a new run starting on 6/22, but 6/23 is allowed
    */
    [Fact]
    public async Task GeneratePayRun_RejectsOverlap_AtInclusiveBoundary_ButAllowsDayAfter()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var clinician = new Clinician("C12", "Test", $"c12_{Guid.NewGuid()}@test.com", false, 0.6);
        var batch = new ImportBatch("file.csv", Guid.NewGuid().ToString());
        var ehrUser = new EHRUser("X", "Y", $"xy_{Guid.NewGuid()}");

        db.Clinicians.Add(clinician);
        db.ImportBatches.Add(batch);
        db.EHRUsers.Add(ehrUser);

        // Isolated historical range so this test's dates don't collide with other tests' pay runs
        var existingStart = new DateTime(2020, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var existingEnd = new DateTime(2020, 6, 22, 0, 0, 0, DateTimeKind.Utc);

        db.PaymentLineItems.Add(CreatePayment(clinician, batch, ehrUser, 100m, existingStart, existingStart, 1));
        await db.SaveChangesAsync();

        var firstRun = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = existingStart, EndDate = existingEnd });
        firstRun.EnsureSuccessStatusCode();

        // Starting the new run on the existing run's end date should be rejected
        var overlapping = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = existingEnd, EndDate = existingEnd.AddDays(5) });
        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);

        // Starting the day after the existing run's end date should be allowed
        var nonOverlapping = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = existingEnd.AddDays(1), EndDate = existingEnd.AddDays(5) });
        Assert.Equal(HttpStatusCode.OK, nonOverlapping.StatusCode);
    }

    /*
    Validate that a pay run whose generation failed, or whose approval was rejected, does not
    block a new pay run from being generated over the same date range
    */
    [Fact]
    public async Task GeneratePayRun_AllowsRetry_OverRangeOfFailedOrRejectedPayRun()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        // Isolated historical range so this test's dates don't collide with other tests' pay runs
        var start = new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2021, 9, 10, 0, 0, 0, DateTimeKind.Utc);

        var failedPayRun = PayRun.GeneratePayRun(start, end);
        failedPayRun.GenerationStatus = PayRunStatusEnum.FAILED;
        db.PayRuns.Add(failedPayRun);
        await db.SaveChangesAsync();

        var res = await _client.PostAsJsonAsync("/api/payrun", new { StartDate = start, EndDate = end });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    /*
    Validate that enabling the Psych Today payout flag without a positive amount returns a 400
    */
    [Fact]
    public async Task GeneratePayRun_WithPsychTodayPayoutEnabled_NoAmount_ReturnsBadRequest()
    {
        var db = await ResetDb();

        var admin = await Signup("admin", db);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", admin.Token);

        var res = await _client.PostAsJsonAsync("/api/payrun", new
        {
            StartDate = new DateTime(2024, 8, 1),
            EndDate = new DateTime(2024, 8, 31),
            IncludePsychTodayPayout = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
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
}
