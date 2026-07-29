using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class PayRunUnitTests
{
    private static UserAccount GetUser(RoleEnum role)
        =>  UserAccount.GenerateUserAccount("winston.heinrichs@example.com", "test", "Winston", "Heinrichs", role, GetClinician());

    private static Clinician GetClinician() => new Clinician("John", "Doe", "john@test.com", false, 0.5);

    private static PaymentSnapshot GetSnapshot(decimal payment, decimal adjustment)
    {
        var item = PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            null,
            "Dr",
            payment,
            adjustment,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "p",
            "cpt",
            "pay",
            "payer",
            new EHRUser("a","b","c"),
            new ImportBatch("f","fp"),
            1,
            "fp",
            DateTime.UtcNow,
            null
        );

        return PaymentSnapshot.CreateSnapshot(item, PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1)));
    }

    private static PayRun GetDraftPayRun()
        => PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

    /*
    Test factory creates valid pay run
    */
    [Fact]
    public void GeneratePayRun_ShouldCreateDraft()
    {
        var payRun = GetDraftPayRun();

        Assert.Equal(ApprovalStateEnum.DRAFT, payRun.ApprovalState);
        Assert.NotEqual(Guid.Empty, payRun.Id);
    }

    /*
    Test factory throws when end date in future
    */
    [Fact]
    public void GeneratePayRun_ShouldThrow_WhenEndDateFuture()
    {
        Assert.Throws<ArgumentException>(() =>
            PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
        );
    }

    /*
    Test AssignPayments works in draft
    */
    [Fact]
    public void AssignPayments_ShouldSetPayments()
    {
        var payRun = GetDraftPayRun();
        var payments = new List<PaymentSnapshot> { GetSnapshot(100, 10) };

        payRun.AssignPayments(payments);

        Assert.Equal(payments, payRun.Payments);
    }

    /*
    Test AssignPayments throws if not draft
    */
    [Fact]
    public void AssignPayments_ShouldThrow_WhenNotDraft()
    {
        var payRun = GetDraftPayRun();
        payRun.CalculateTotals();

        Assert.Throws<InvalidOperationException>(() =>
            payRun.AssignPayments(new List<PaymentSnapshot>())
        );
    }

    /*
    Test AssignPayments cannot reassign
    */
    [Fact]
    public void AssignPayments_ShouldThrow_WhenAlreadyAssigned()
    {
        var payRun = GetDraftPayRun();
        var payments = new List<PaymentSnapshot> { GetSnapshot(100, 10) };

        payRun.AssignPayments(payments);

        Assert.Throws<InvalidOperationException>(() =>
            payRun.AssignPayments(payments)
        );
    }

    /*
    Test CalculateTotals computes totals and updates state
    */
    [Fact]
    public void CalculateTotals_ShouldComputeAndTransitionState()
    {
        var payRun = GetDraftPayRun();

        var payments = new List<PaymentSnapshot>
        {
            GetSnapshot(100, 10),
            GetSnapshot(200, 20)
        };

        payRun.AssignPayments(payments);

        payRun.CalculateTotals();

        Assert.Equal(300, payRun.TotalApplied);
        Assert.Equal(30, payRun.TotalAdjudicated);
        Assert.Equal(0, payRun.TotalPsychTodayPayout);
        Assert.Equal(payRun.StatementTotals, payRun.TotalPayout);
        Assert.Equal(ApprovalStateEnum.PENDING, payRun.ApprovalState);
        Assert.Equal(PayRunStatusEnum.COMPLETED, payRun.GenerationStatus);
    }

    /*
    Test CalculateTotals sums each statement's Psych Today payout into the run-level total and includes it in TotalPayout
    */
    [Fact]
    public void CalculateTotals_ShouldSumPsychTodayPayoutAcrossStatements()
    {
        var payRun = GetDraftPayRun();
        payRun.AssignPayments(new List<PaymentSnapshot>());

        var clinician = new Clinician("John", "Doe", "john@test.com", true, 0.5);
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);
        statement.ApplyPsychTodayPayout(50m);
        statement.CalculateTotals();
        payRun.Statements.Add(statement);

        payRun.CalculateTotals();

        Assert.Equal(50m, payRun.TotalPsychTodayPayout);
        Assert.Equal(payRun.StatementTotals + 50m, payRun.TotalPayout);
    }

    /*
    Test CalculateTotals throws if not draft
    */
    [Fact]
    public void CalculateTotals_ShouldThrow_WhenNotDraft()
    {
        var payRun = GetDraftPayRun();

        payRun.AssignPayments(new List<PaymentSnapshot>());
        payRun.CalculateTotals(); // now PENDING

        Assert.Throws<InvalidOperationException>(() =>
            payRun.CalculateTotals()
        );
    }

    /*
    Test Approve works for valid roles
    */
    [Theory]
    [InlineData(RoleEnum.ADMIN)]
    [InlineData(RoleEnum.BACKEND)]
    public void Approve_ShouldWork_ForValidRoles(RoleEnum role)
    {
        var payRun = GetDraftPayRun();
        payRun.AssignPayments(new List<PaymentSnapshot>());
        payRun.CalculateTotals();

        var user = GetUser(role);

        payRun.Approve(user);

        Assert.Equal(ApprovalStateEnum.APPROVED, payRun.ApprovalState);
        Assert.Equal(user.Id, payRun.ApprovedRejectedBy);
        Assert.NotNull(payRun.ApprovedRejectedOn);
    }

    /*
    Test Approve throws for invalid role
    */
    [Fact]
    public void Approve_ShouldThrow_WhenInvalidRole()
    {
        var payRun = GetDraftPayRun();
        payRun.AssignPayments(new List<PaymentSnapshot>());
        payRun.CalculateTotals();

        var user = GetUser(RoleEnum.CLINICIAN);

        Assert.Throws<InvalidOperationException>(() =>
            payRun.Approve(user)
        );
    }

    /*
    Test Approve throws if not pending
    */
    [Fact]
    public void Approve_ShouldThrow_WhenNotPending()
    {
        var payRun = GetDraftPayRun();
        var user = GetUser(RoleEnum.ADMIN);

        Assert.Throws<InvalidOperationException>(() =>
            payRun.Approve(user)
        );
    }

    /*
    Test Reject sets state
    */
    [Fact]
    public void Reject_ShouldSetState()
    {
        var payRun = GetDraftPayRun();
        var user = GetUser(RoleEnum.ADMIN);

        payRun.Reject(user);

        Assert.Equal(ApprovalStateEnum.REJECTED, payRun.ApprovalState);
        Assert.Equal(user.Id, payRun.ApprovedRejectedBy);
        Assert.NotNull(payRun.ApprovedRejectedOn);
    }

    /*
    Test Reject throws for invalid role
    */
    [Fact]
    public void Reject_ShouldThrow_WhenInvalidRole()
    {
        var payRun = GetDraftPayRun();
        var user = GetUser(RoleEnum.CLINICIAN);

        Assert.Throws<InvalidOperationException>(() =>
            payRun.Reject(user)
        );
    }

    /*
    Test EnsureEditable allows draft and pending
    */
    [Fact]
    public void EnsureEditable_ShouldAllowDraftAndPending()
    {
        var payRun = GetDraftPayRun();

        payRun.EnsureEditable(); // DRAFT OK

        payRun.AssignPayments(new List<PaymentSnapshot>());
        payRun.CalculateTotals(); // PENDING

        payRun.EnsureEditable(); // still OK
    }

    /*
    Test EnsureEditable throws when approved/rejected
    */
    [Fact]
    public void EnsureEditable_ShouldThrow_WhenNotEditable()
    {
        var payRun = GetDraftPayRun();
        var user = GetUser(RoleEnum.ADMIN);

        payRun.Reject(user);

        Assert.Throws<InvalidOperationException>(() =>
            payRun.EnsureEditable()
        );
    }

    /*
    Test that an explicitly provided payment date is stored on the pay run
    */
    [Fact]
    public void GeneratePayRun_ShouldUseProvidedPaymentDate()
    {
        var paymentDate = DateTime.SpecifyKind(new DateTime(2026, 8, 15), DateTimeKind.Utc);

        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1), paymentDate);

        Assert.Equal(paymentDate, payRun.PaymentDate);
    }

    /*
    Test that omitting the payment date defaults to the next 1st or 15th
    */
    [Fact]
    public void GeneratePayRun_ShouldDefaultPaymentDate_WhenNotProvided()
    {
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

        Assert.Equal(PayRun.NextDefaultPaymentDate(DateTime.UtcNow), payRun.PaymentDate);
    }

    /*
    Test the default payment date rule: the next 1st or 15th strictly after the given date
    */
    [Theory]
    [InlineData(2026, 7, 28, 2026, 8, 1)]   // late month -> 1st of next month
    [InlineData(2026, 7, 10, 2026, 7, 15)]  // early month -> 15th of same month
    [InlineData(2026, 7, 1, 2026, 7, 15)]   // on the 1st -> the upcoming 15th
    [InlineData(2026, 7, 15, 2026, 8, 1)]   // on the 15th -> 1st of next month
    [InlineData(2026, 12, 20, 2027, 1, 1)]  // year rollover
    public void NextDefaultPaymentDate_ShouldPickClosestFuture1stOr15th(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var from = DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

        var result = PayRun.NextDefaultPaymentDate(from);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay), result.Date);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    /*
    TODO ADD IN A TEST THAT TESTS THE TOTAL AMOUNT FROM THE STATEMENTS MATCHES THE TOTAL AMOUNT FROM THE PAYRUN
    */
}