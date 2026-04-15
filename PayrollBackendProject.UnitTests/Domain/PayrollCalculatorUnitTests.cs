using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Domain.Service;


namespace PayrollBackendProject.UnitTests;

public class PayrollCalculatorUnitTests
{
    private static Clinician GetClinician()
        => new Clinician("John", "Doe", "john@test.com", false, 0.5);

    private static PayRun GetPayRun()
        => PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

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

        return PaymentSnapshot.CreateSnapshot(item, GetPayRun());
    }

    /*
    Test happy path: generates valid statement with correct totals and links
    */
    [Fact]
    public void GeneratePayroll_ShouldCreateValidStatement()
    {
        var calculator = new PayrollCalculator();

        var clinician = GetClinician();
        var payRun = GetPayRun();

        var snapshots = new List<PaymentSnapshot>
        {
            GetSnapshot(clinician, 100, 10),
            GetSnapshot(clinician, 200, 20)
        };

        var statement = calculator.GeneratePayroll(snapshots, clinician, payRun);

        Assert.Equal(2, statement.LineItems.Count);
        Assert.Equal(300, statement.TotalPayment);
        Assert.Equal(30, statement.TotalAdjustment);
        Assert.Equal(ApprovalStateEnum.PENDING, statement.ApprovalState);

        // Ensure snapshots are linked back
        foreach (var s in snapshots)
        {
            Assert.Equal(statement.Id, s.PayStatementId);
        }
    }

    /*
    Test that clinician mismatch is rejected (domain invariant enforced)
    */
    [Fact]
    public void GeneratePayroll_ShouldThrow_WhenClinicianMismatch()
    {
        var calculator = new PayrollCalculator();

        var clinician = GetClinician();
        var payRun = GetPayRun();

        var otherClinician = new Clinician("Jane", "Doe", "jane@test.com", false, 0.5);

        var snapshots = new List<PaymentSnapshot>
        {
            GetSnapshot(otherClinician, 100, 10)
        };

        Assert.Throws<InvalidOperationException>(() =>
            calculator.GeneratePayroll(snapshots, clinician, payRun)
        );
    }

    /*
    Test empty input: should return valid empty statement
    */
    [Fact]
    public void GeneratePayroll_ShouldHandleEmptyPayments()
    {
        var calculator = new PayrollCalculator();

        var clinician = GetClinician();
        var payRun = GetPayRun();

        var statement = calculator.GeneratePayroll(new List<PaymentSnapshot>(), clinician, payRun);

        Assert.Empty(statement.LineItems);
        Assert.Equal(0, statement.TotalPayment);
        Assert.Equal(0, statement.TotalAdjustment);
        Assert.Equal(ApprovalStateEnum.PENDING, statement.ApprovalState);
    }
}