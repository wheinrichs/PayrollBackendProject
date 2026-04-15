using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class PayStatementUnitTests
{
    private static Clinician GetClinician()
        => new Clinician("John", "Doe", "john@test.com", false, 0.5);

    private static PayRun GetPayRun()
        => PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

    private static UserAccount GetUser(RoleEnum role)
        => UserAccount.GenerateUserAccount("winston.heinrichs@example.com", "test", "Winston", "Heinrichs", role, GetClinician());

    private static PaymentSnapshot GetSnapshot(Clinician clinician, decimal payment, decimal adjustment)
    {
        var item = PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            "Dr",
            payment,
            adjustment,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "p",
            "cpt",
            "pay",
            "payer",
            new EHRUser("a", "b", "c"),
            new ImportBatch("f", "fp"),
            1,
            "fp",
            DateTime.UtcNow,
            null
        );

        var payRun = GetPayRun();
        return PaymentSnapshot.CreateSnapshot(item, payRun);
    }

    /*
    Test factory creates valid draft statement
    */
    [Fact]
    public void GenerateDraftPayStatement_ShouldCreateDraft()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();

        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        Assert.Equal(ApprovalStateEnum.DRAFT, statement.ApprovalState);
        Assert.Equal(payRun.Id, statement.PayRunId);
        Assert.Equal((decimal)clinician.CostShare, statement.ClinicianCostShare);
        Assert.NotEqual(Guid.Empty, statement.Id);
    }

    /*
    Test factory throws when null inputs
    */
    [Fact]
    public void GenerateDraftPayStatement_ShouldThrow_WhenNull()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();

        Assert.Throws<ArgumentException>(() =>
            PayStatement.GenerateDraftPayStatement(null!, payRun)
        );

        Assert.Throws<ArgumentException>(() =>
            PayStatement.GenerateDraftPayStatement(clinician, null!)
        );
    }

    /*
    Test AddPaymentLineItem works in draft
    */
    [Fact]
    public void AddPaymentLineItem_ShouldAdd_WhenValid()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        var snapshot = GetSnapshot(clinician, 100, 10);

        statement.AddPaymentLineItem(snapshot);

        Assert.Single(statement.LineItems);
    }

    /*
    Test AddPaymentLineItem throws when not draft
    */
    [Fact]
    public void AddPaymentLineItem_ShouldThrow_WhenNotDraft()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        statement.CalculateTotals(); // now PENDING

        var snapshot = GetSnapshot(clinician, 100, 10);

        Assert.Throws<InvalidOperationException>(() =>
            statement.AddPaymentLineItem(snapshot)
        );
    }

    /*
    Test AddPaymentLineItem throws when clinician mismatch
    */
    [Fact]
    public void AddPaymentLineItem_ShouldThrow_WhenClinicianMismatch()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        var otherClinician = new Clinician("Jane", "Doe", "jane@test.com", false, 0.5);
        var snapshot = GetSnapshot(otherClinician, 100, 10);

        Assert.Throws<InvalidOperationException>(() =>
            statement.AddPaymentLineItem(snapshot)
        );
    }

    /*
    Test CalculateTotals computes correctly and transitions state
    */
    [Fact]
    public void CalculateTotals_ShouldComputeAndTransition()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        var snapshot1 = GetSnapshot(clinician, 100, 10);
        var snapshot2 = GetSnapshot(clinician, 200, 20);

        statement.AddPaymentLineItem(snapshot1);
        statement.AddPaymentLineItem(snapshot2);

        statement.CalculateTotals();

        Assert.Equal(300, statement.TotalPayment);
        Assert.Equal(30, statement.TotalAdjustment);
        Assert.Equal(300 * (decimal)clinician.CostShare, statement.CostShareAdjustedPayment);
        Assert.Equal(ApprovalStateEnum.PENDING, statement.ApprovalState);
    }

    /*
    Test CalculateTotals throws when not draft
    */
    [Fact]
    public void CalculateTotals_ShouldThrow_WhenNotDraft()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        statement.CalculateTotals();

        Assert.Throws<InvalidOperationException>(() =>
            statement.CalculateTotals()
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
        var statement = SetupPendingStatement();

        var user = GetUser(role);

        statement.Approve(user);

        Assert.Equal(ApprovalStateEnum.APPROVED, statement.ApprovalState);
        Assert.Equal(user.Id, statement.ApprovedRejectedBy);
        Assert.NotNull(statement.ApprovedRejectedOn);
    }

    /*
    Test Approve throws when invalid role
    */
    [Fact]
    public void Approve_ShouldThrow_WhenInvalidRole()
    {
        var statement = SetupPendingStatement();

        var user = GetUser(RoleEnum.CLINICIAN);

        Assert.Throws<InvalidOperationException>(() =>
            statement.Approve(user)
        );
    }

    /*
    Test Approve throws when not pending
    */
    [Fact]
    public void Approve_ShouldThrow_WhenNotPending()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        var user = GetUser(RoleEnum.ADMIN);

        Assert.Throws<InvalidOperationException>(() =>
            statement.Approve(user)
        );
    }

    /*
    Test Reject sets state
    */
    [Fact]
    public void Reject_ShouldSetState()
    {
        var statement = SetupPendingStatement();
        var user = GetUser(RoleEnum.ADMIN);

        statement.Reject(user);

        Assert.Equal(ApprovalStateEnum.REJECTED, statement.ApprovalState);
        Assert.Equal(user.Id, statement.ApprovedRejectedBy);
        Assert.NotNull(statement.ApprovedRejectedOn);
    }

    /*
    Test Reject throws for invalid role
    */
    [Fact]
    public void Reject_ShouldThrow_WhenInvalidRole()
    {
        var statement = SetupPendingStatement();
        var user = GetUser(RoleEnum.CLINICIAN);

        Assert.Throws<InvalidOperationException>(() =>
            statement.Reject(user)
        );
    }

    /*
    Test EnsureEditable allows draft and pending
    */
    [Fact]
    public void EnsureEditable_ShouldAllowDraftAndPending()
    {
        var statement = SetupPendingStatement();

        statement.EnsureEditable(); // PENDING OK
    }

    /*
    Test EnsureEditable throws when approved/rejected
    */
    [Fact]
    public void EnsureEditable_ShouldThrow_WhenNotEditable()
    {
        var statement = SetupPendingStatement();
        var user = GetUser(RoleEnum.ADMIN);

        statement.Reject(user);

        Assert.Throws<InvalidOperationException>(() =>
            statement.EnsureEditable()
        );
    }

    // ------------------------
    // Helpers
    // ------------------------

    private static PayStatement SetupPendingStatement()
    {
        var clinician = GetClinician();
        var payRun = GetPayRun();
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);

        var snapshot = GetSnapshot(clinician, 100, 10);
        statement.AddPaymentLineItem(snapshot);

        statement.CalculateTotals(); // moves to PENDING

        return statement;
    }
}