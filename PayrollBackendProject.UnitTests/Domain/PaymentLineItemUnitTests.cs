using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class PaymentLineItemUnitTests
{
    private static EHRUser GetValidUser() => new EHRUser("John", "Doe", "jdoe");
    private static ImportBatch GetValidBatch() => new ImportBatch("file.csv", "fingerprint");
    private static Clinician GetValidClinician() => new Clinician("John", "Doe", "john@test.com", false, 0.5);

    /*
    Test that GeneratePaymentLineItem sets all provided values correctly
    Test both clinician null and non-null paths
    */
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneratePaymentLineItem_ShouldSetValues(bool includeClinician)
    {
        var user = GetValidUser();
        var batch = GetValidBatch();
        var clinician = includeClinician ? GetValidClinician() : null;

        var item = PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            "Dr. Smith",
            100,
            10,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "patient1",
            "CPT1",
            "payment1",
            "payer1",
            user,
            batch,
            1,
            "fingerprint",
            DateTime.UtcNow,
            null
        );

        Assert.Equal("raw", item.RawData);
        Assert.Equal("Dr. Smith", item.RawClinicianName);
        Assert.Equal(100, item.PaymentAmount);
        Assert.Equal(10, item.AdjustmentAmount);
        Assert.Equal(user.Id, item.AppliedById);
        Assert.Equal(batch.Id, item.ImportBatchId);
        Assert.Equal("fingerprint", item.Fingerprint);

        if (includeClinician)
        {
            Assert.Equal(PaymentLineItemStatusEnum.VALID, item.PaymentLineItemStatus);
            Assert.NotNull(item.ClinicianId);
        }
        else
        {
            Assert.Equal(PaymentLineItemStatusEnum.UNRESOLVED_CLINICIAN, item.PaymentLineItemStatus);
            Assert.Null(item.ClinicianId);
        }
    }

    /*
    Test that a valid Guid is assigned
    */
    [Fact]
    public void GeneratePaymentLineItem_ShouldAssignValidGuid()
    {
        var item = CreateValidItem();

        Assert.NotEqual(Guid.Empty, item.Id);
    }

    /*
    Test that null dependencies throw
    */
    [Fact]
    public void GeneratePaymentLineItem_ShouldThrow_WhenAppliedByNull()
    {
        var batch = GetValidBatch();

        Assert.Throws<ArgumentNullException>(() =>
            PaymentLineItem.GeneratePaymentLineItem(
                "raw",
                null,
                "Dr",
                100,
                0,
                PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
                DateTime.UtcNow,
                "p",
                "cpt",
                "pay",
                "payer",
                null!,
                batch,
                1,
                "fingerprint",
                DateTime.UtcNow,
                null
            )
        );
    }

    [Fact]
    public void GeneratePaymentLineItem_ShouldThrow_WhenImportBatchNull()
    {
        var user = GetValidUser();

        Assert.Throws<ArgumentNullException>(() =>
            PaymentLineItem.GeneratePaymentLineItem(
                "raw",
                null,
                "Dr",
                100,
                0,
                PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
                DateTime.UtcNow,
                "p",
                "cpt",
                "pay",
                "payer",
                user,
                null!,
                1,
                "fingerprint",
                DateTime.UtcNow,
                null
            )
        );
    }

    /*
    Test that invalid string inputs throw
    */
    [Theory]
    [InlineData("", "Dr", "fingerprint")]
    [InlineData("raw", "", "fingerprint")]
    [InlineData("raw", "Dr", "")]
    public void GeneratePaymentLineItem_ShouldThrow_WhenInvalidStrings(string raw, string clinicianName, string fingerprint)
    {
        var user = GetValidUser();
        var batch = GetValidBatch();

        Assert.Throws<ArgumentException>(() =>
            PaymentLineItem.GeneratePaymentLineItem(
                raw,
                null,
                clinicianName,
                100,
                0,
                PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
                DateTime.UtcNow,
                "p",
                "cpt",
                "pay",
                "payer",
                user,
                batch,
                1,
                fingerprint,
                DateTime.UtcNow,
                null
            )
        );
    }

    /*
    Test UpdateClinician sets clinician and status correctly
    */
    [Fact]
    public void UpdateClinician_ShouldSetClinicianAndStatus()
    {
        var item = CreateValidItemWithNullClinician();
        var clinician = GetValidClinician();

        item.UpdateClinician(clinician);

        Assert.Equal(clinician.ID, item.ClinicianId);
        Assert.Equal(PaymentLineItemStatusEnum.VALID, item.PaymentLineItemStatus);
    }

    /*
    Test UpdateClinician throws when null
    */
    [Fact]
    public void UpdateClinician_ShouldThrow_WhenNull()
    {
        var item = CreateValidItem();

        Assert.Throws<ArgumentNullException>(() =>
            item.UpdateClinician(null!)
        );
    }

    /*
    Test that a partial apply reduces the remaining balance without marking the line item
    as fully applied, and records a single application entry
    */
    [Fact]
    public void ApplyCode500_ShouldReduceRemainingBalance_WhenPartial()
    {
        var item = CreateValidTakebackItem(-200m);
        var payRun = CreateValidPayRun();

        Code500Application application = item.ApplyCode500(50m, Guid.NewGuid(), payRun);

        Assert.Equal(50m, application.AppliedAmount);
        Assert.Equal(150m, item.RemainingCode500Amount);
        Assert.False(item.IsCode500Applied);
        Assert.Single(item.Code500Applications);
    }

    /*
    Test that two sequential partial applications summing to the full magnitude
    mark the line item fully applied on the second call
    */
    [Fact]
    public void ApplyCode500_ShouldMarkFullyApplied_AfterSequentialPartialApplications()
    {
        var item = CreateValidTakebackItem(-200m);
        var payRun = CreateValidPayRun();

        item.ApplyCode500(50m, Guid.NewGuid(), payRun);
        Assert.False(item.IsCode500Applied);

        item.ApplyCode500(150m, Guid.NewGuid(), payRun);

        Assert.True(item.IsCode500Applied);
        Assert.Equal(0m, item.RemainingCode500Amount);
        Assert.Equal(2, item.Code500Applications.Count);
    }

    /*
    Test that applying more than the remaining outstanding balance throws
    */
    [Fact]
    public void ApplyCode500_ShouldThrow_WhenAmountExceedsRemaining()
    {
        var item = CreateValidTakebackItem(-200m);
        var payRun = CreateValidPayRun();

        Assert.Throws<ArgumentException>(() => item.ApplyCode500(201m, Guid.NewGuid(), payRun));
    }

    /*
    Test that applying a non-positive amount throws
    */
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ApplyCode500_ShouldThrow_WhenAmountNotPositive(decimal amount)
    {
        var item = CreateValidTakebackItem(-200m);
        var payRun = CreateValidPayRun();

        Assert.Throws<ArgumentException>(() => item.ApplyCode500(amount, Guid.NewGuid(), payRun));
    }

    /*
    Test that applying to a line item with no resolved clinician throws
    */
    [Fact]
    public void ApplyCode500_ShouldThrow_WhenClinicianUnresolved()
    {
        var item = CreateValidTakebackItem(-200m, includeClinician: false);
        var payRun = CreateValidPayRun();

        Assert.Throws<InvalidOperationException>(() => item.ApplyCode500(50m, Guid.NewGuid(), payRun));
    }

    /*
    Test that applying to a non-500-code line item throws
    */
    [Fact]
    public void ApplyCode500_ShouldThrow_WhenNotTakebackCode()
    {
        var item = CreateValidItem();
        var payRun = CreateValidPayRun();

        Assert.Throws<InvalidOperationException>(() => item.ApplyCode500(10m, Guid.NewGuid(), payRun));
    }

    /*
    Test that rejecting an unapplied takeback item marks it rejected with a timestamp and actor
    */
    [Fact]
    public void Reject_ShouldMarkRejected_WhenNothingApplied()
    {
        var item = CreateValidTakebackItem(-200m);
        var userId = Guid.NewGuid();

        item.Reject(userId);

        Assert.True(item.IsRejected);
        Assert.Equal(userId, item.RejectedById);
        Assert.NotNull(item.RejectedDate);
    }

    /*
    Test that rejecting a non-500-code line item throws
    */
    [Fact]
    public void Reject_ShouldThrow_WhenNotTakebackCode()
    {
        var item = CreateValidItem();

        Assert.Throws<InvalidOperationException>(() => item.Reject(Guid.NewGuid()));
    }

    /*
    Test that rejecting an item that already has an applied amount throws
    */
    [Fact]
    public void Reject_ShouldThrow_WhenAmountAlreadyApplied()
    {
        var item = CreateValidTakebackItem(-200m);
        var payRun = CreateValidPayRun();
        item.ApplyCode500(50m, Guid.NewGuid(), payRun);

        Assert.Throws<InvalidOperationException>(() => item.Reject(Guid.NewGuid()));
    }

    /*
    Test that rejecting an already-rejected item throws
    */
    [Fact]
    public void Reject_ShouldThrow_WhenAlreadyRejected()
    {
        var item = CreateValidTakebackItem(-200m);
        item.Reject(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => item.Reject(Guid.NewGuid()));
    }

    // ------------------------
    // Helpers
    // ------------------------

    private static PayRun CreateValidPayRun() =>
        PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddDays(-1));

    private static PaymentLineItem CreateValidTakebackItem(decimal adjustmentAmount, bool includeClinician = true)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            includeClinician ? GetValidClinician() : null,
            "Dr",
            0,
            adjustmentAmount,
            PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK,
            DateTime.UtcNow,
            "p",
            "cpt",
            "pay",
            "payer",
            GetValidUser(),
            GetValidBatch(),
            1,
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            null
        );
    }

    private static PaymentLineItem CreateValidItem()
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            GetValidClinician(),
            "Dr",
            100,
            0,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "p",
            "cpt",
            "pay",
            "payer",
            GetValidUser(),
            GetValidBatch(),
            1,
            "fingerprint",
            DateTime.UtcNow,
            null
        );
    }

    private static PaymentLineItem CreateValidItemWithNullClinician()
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            null,
            "Dr",
            100,
            0,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow,
            "p",
            "cpt",
            "pay",
            "payer",
            GetValidUser(),
            GetValidBatch(),
            1,
            "fingerprint",
            DateTime.UtcNow,
            null
        );
    }
}