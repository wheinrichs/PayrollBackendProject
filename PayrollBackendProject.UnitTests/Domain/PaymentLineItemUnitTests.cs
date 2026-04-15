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

    // ------------------------
    // Helpers
    // ------------------------

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