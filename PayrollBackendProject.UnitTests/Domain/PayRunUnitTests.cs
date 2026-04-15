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
        Assert.Equal(ApprovalStateEnum.PENDING, payRun.ApprovalState);
        Assert.Equal(PayRunStatusEnum.COMPLETED, payRun.GenerationStatus);
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
    TODO ADD IN A TEST THAT TESTS THE TOTAL AMOUNT FROM THE STATEMENTS MATCHES THE TOTAL AMOUNT FROM THE PAYRUN
    */
}