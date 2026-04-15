using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class PaymentSnapshotUnitTests
{
    private static EHRUser GetUser() => new EHRUser("John", "Doe", "jdoe");
    private static ImportBatch GetBatch() => new ImportBatch("file.csv", "fingerprint");
    private static Clinician GetClinician() => new Clinician("John", "Doe", "john@test.com", false, 0.5);
    private static PayRun GetPayRun() => PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow);
    private static PayStatement GetStatement(Clinician clinician, PayRun payrun) => PayStatement.GenerateDraftPayStatement(clinician, payrun);

    private static PaymentLineItem GetLineItem(bool includeClinician = true)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            includeClinician ? GetClinician() : null,
            "Dr",
            100,
            10,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "patient",
            "cpt",
            "payment",
            "payer",
            GetUser(),
            GetBatch(),
            1,
            "fingerprint",
            DateTime.UtcNow,
            null
        );
    }

    /*
    Test CreateSnapshot copies all relevant fields correctly
    Test both clinician null and non-null paths
    */
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateSnapshot_ShouldCopyFields(bool includeClinician)
    {
        var lineItem = GetLineItem(includeClinician);
        var payRun = GetPayRun();

        var snapshot = PaymentSnapshot.CreateSnapshot(lineItem, payRun);

        Assert.Equal(lineItem.Id, snapshot.PaymentLineItemId);
        Assert.Equal(payRun.Id, snapshot.PayRunId);

        Assert.Equal(lineItem.RawData, snapshot.RawData);
        Assert.Equal(lineItem.ClinicianId, snapshot.ClinicianId);
        Assert.Equal(lineItem.PaymentAmount, snapshot.PaymentAmount);
        Assert.Equal(lineItem.AdjustmentAmount, snapshot.AdjustmentAmount);
        Assert.Equal(lineItem.PaymentAdjustmentCode, snapshot.AdjustmentCode);
        Assert.Equal(lineItem.DateOfService, snapshot.DateOfService);
        Assert.Equal(lineItem.PatientId, snapshot.PatientId);
        Assert.Equal(lineItem.CPTCode, snapshot.CPTCode);
        Assert.Equal(lineItem.PaymentId, snapshot.PaymentId);
        Assert.Equal(lineItem.Payer, snapshot.Payer);
        Assert.Equal(lineItem.AppliedById, snapshot.AppliedById);
        Assert.Equal(lineItem.ImportBatchId, snapshot.ImportBatchId);
        Assert.Equal(lineItem.RowNumber, snapshot.RowNumber);
        Assert.Equal(lineItem.AppliedDate, snapshot.AppliedDate);
        Assert.Equal(lineItem.PaymentDate, snapshot.PaymentDate);
    }

    /*
    Test snapshot assigns a valid Guid
    */
    [Fact]
    public void CreateSnapshot_ShouldAssignValidGuid()
    {
        var snapshot = PaymentSnapshot.CreateSnapshot(GetLineItem(), GetPayRun());

        Assert.NotEqual(Guid.Empty, snapshot.Id);
    }

    /*
    Test CreateSnapshot throws when inputs are null
    */
    [Fact]
    public void CreateSnapshot_ShouldThrow_WhenLineItemNull()
    {
        var payRun = GetPayRun();

        Assert.Throws<ArgumentException>(() =>
            PaymentSnapshot.CreateSnapshot(null!, payRun)
        );
    }

    [Fact]
    public void CreateSnapshot_ShouldThrow_WhenPayRunNull()
    {
        var lineItem = GetLineItem();

        Assert.Throws<ArgumentException>(() =>
            PaymentSnapshot.CreateSnapshot(lineItem, null!)
        );
    }

    /*
    Test AssignStatement sets values correctly
    */
    [Fact]
    public void AssignStatement_ShouldSetStatement()
    {
        var lineItem = GetLineItem();
        var payRun = GetPayRun();
        var snapshot = PaymentSnapshot.CreateSnapshot(lineItem, payRun);
        var statement = GetStatement(GetClinician(), payRun);

        snapshot.AssignStatement(statement);

        Assert.Equal(statement, snapshot.PayStatement);
        Assert.Equal(statement.Id, snapshot.PayStatementId);
    }

    /*
    Test AssignStatement throws if already assigned
    */
    [Fact]
    public void AssignStatement_ShouldThrow_WhenAlreadyAssigned()
    {
        var lineItem = GetLineItem();
        var payRun = GetPayRun();
        var snapshot = PaymentSnapshot.CreateSnapshot(lineItem, payRun);
        var statement = GetStatement(GetClinician(), payRun);


        snapshot.AssignStatement(statement);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.AssignStatement(statement)
        );
    }

    /*
    Test snapshot is independent of original PaymentLineItem (immutability guarantee)
    */
    [Fact]
    public void Snapshot_ShouldNotChange_WhenOriginalLineItemChanges()
    {
        var lineItem = GetLineItem(includeClinician: false);
        var payRun = GetPayRun();

        var snapshot = PaymentSnapshot.CreateSnapshot(lineItem, payRun);

        var originalClinicianId = snapshot.ClinicianId;

        // mutate original object
        lineItem.UpdateClinician(GetClinician());

        // snapshot should remain unchanged
        Assert.Equal(originalClinicianId, snapshot.ClinicianId);
        Assert.NotEqual(lineItem.ClinicianId, snapshot.ClinicianId);
    }
}