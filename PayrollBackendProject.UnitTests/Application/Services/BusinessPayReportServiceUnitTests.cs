using Moq;
using Xunit;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Service;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.DTO;

public class BusinessPayReportServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();

    private readonly PayrollCalculator _calculator = new();

    private BusinessPayReportService CreateService()
    {
        _paymentRepo
            .Setup(r => r.GetCode500ApplicationsBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Code500Application>());

        return new BusinessPayReportService(
            _paymentRepo.Object,
            _clinicianRepo.Object,
            _calculator
        );
    }

    /*
    Validate that the report's per-clinician payout preview matches the exact math a real pay run
    would produce for the same automatically-flowing payments (cost share applied, no persistence)
    */
    [Fact]
    public async Task GenerateReport_ShouldPreviewPayoutTotals_MatchingPayRunMath()
    {
        var service = CreateService();
        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 1000m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new BusinessPayReportRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var result = await service.GenerateReport(request);

        Assert.Equal(1000m, result.GrossPaymentTotal);
        Assert.Equal(600m, result.StatementTotals);
        Assert.Equal(600m, result.TotalPayout);

        var entry = Assert.Single(result.ClinicianBreakdown);
        Assert.Equal(clinician.ID, entry.ClinicianId);
        Assert.Equal(600m, entry.CostShareAdjustedPayment);
        Assert.Equal(0m, entry.Code500Deductions);
        Assert.Equal(0, entry.Code500LineItemCount);
    }

    /*
    Validate that code-500 (INSURANCE_TAKEBACK) line items never flow into the automatic payment
    totals - they only ever appear via the historical code-500 application lookup
    */
    [Fact]
    public async Task GenerateReport_ShouldExcludeCode500LineItems_FromAutomaticTotals()
    {
        var service = CreateService();
        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);

        var normalPayment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);
        var takeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { normalPayment, takeback });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new BusinessPayReportRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var result = await service.GenerateReport(request);

        Assert.Equal(500m, result.GrossPaymentTotal);
        Assert.Equal(300m, result.StatementTotals);
    }

    /*
    Validate that historical code-500 applications (from real, already-completed pay runs) are
    reported as informational totals/counts, without being netted into the payout preview
    */
    [Fact]
    public async Task GenerateReport_ShouldReportHistoricalCode500Deductions_WithoutAffectingPayout()
    {
        var service = CreateService();
        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 1000m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var historicalTakeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);
        var historicalPayRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-20));
        var application = Code500Application.Create(historicalTakeback, 200m, Guid.NewGuid(), historicalPayRun);

        _paymentRepo
            .Setup(r => r.GetCode500ApplicationsBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Code500Application> { application });

        var request = new BusinessPayReportRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var result = await service.GenerateReport(request);

        Assert.Equal(200m, result.TotalCode500Deductions);
        Assert.Equal(1, result.TotalCode500LineItemCount);
        // The historical deduction does not reduce this range's automatic payout preview
        Assert.Equal(600m, result.StatementTotals);

        var entry = Assert.Single(result.ClinicianBreakdown);
        Assert.Equal(200m, entry.Code500Deductions);
        Assert.Equal(1, entry.Code500LineItemCount);
        Assert.Equal(600m, entry.CostShareAdjustedPayment);
    }

    /*
    Validate that a clinician with only a historical code-500 application (no automatic payments
    in range) still appears in the breakdown, rather than being silently dropped
    */
    [Fact]
    public async Task GenerateReport_ShouldIncludeClinician_WithOnlyHistoricalCode500()
    {
        var service = CreateService();
        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem>());

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var historicalTakeback = GeneratePaymentLineItem(clinician, 0m, -100m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);
        var historicalPayRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-20));
        var application = Code500Application.Create(historicalTakeback, 100m, Guid.NewGuid(), historicalPayRun);

        _paymentRepo
            .Setup(r => r.GetCode500ApplicationsBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Code500Application> { application });

        var request = new BusinessPayReportRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var result = await service.GenerateReport(request);

        var entry = Assert.Single(result.ClinicianBreakdown);
        Assert.Equal(clinician.ID, entry.ClinicianId);
        Assert.Equal(0m, entry.TotalPayment);
        Assert.Equal(0m, entry.CostShareAdjustedPayment);
        Assert.Equal(100m, entry.Code500Deductions);
        Assert.Equal(1, entry.Code500LineItemCount);
    }

    /*
    Validate that an eligible clinician (HasPsychToday) receives the previewed flat Psych Today
    payout when the flag is enabled, reflected in both the run-level and per-clinician totals
    */
    [Fact]
    public async Task GenerateReport_ShouldApplyPsychTodayPayout_ForEligibleClinicianOnly()
    {
        var service = CreateService();
        var eligible = new Clinician("A", "B", "AB@AB.com", true, 0.6);
        var ineligible = new Clinician("C", "D", "CD@CD.com", false, 0.6);

        var eligiblePayment = GeneratePaymentLineItem(eligible, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);
        var ineligiblePayment = GeneratePaymentLineItem(ineligible, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { eligiblePayment, ineligiblePayment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { eligible, ineligible });

        var request = new BusinessPayReportRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = 50m
        };

        var result = await service.GenerateReport(request);

        Assert.Equal(50m, result.TotalPsychTodayPayout);
        Assert.Equal(result.StatementTotals + 50m, result.TotalPayout);

        var eligibleEntry = result.ClinicianBreakdown.Single(e => e.ClinicianId == eligible.ID);
        var ineligibleEntry = result.ClinicianBreakdown.Single(e => e.ClinicianId == ineligible.ID);

        Assert.Equal(50m, eligibleEntry.PsychTodayPayout);
        Assert.Equal(0m, ineligibleEntry.PsychTodayPayout);
    }

    /*
    Validate that enabling the Psych Today payout flag without a positive amount throws
    */
    [Fact]
    public async Task GenerateReport_ShouldThrow_WhenPsychTodayPayoutEnabledWithoutAmount()
    {
        var service = CreateService();

        var request = new BusinessPayReportRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateReport(request));
    }

    /*
    Validate that reports over the same or overlapping date range can be generated repeatedly -
    unlike pay runs, reports have no overlap restriction
    */
    [Fact]
    public async Task GenerateReport_ShouldAllowRepeatedOverlappingRanges()
    {
        var service = CreateService();
        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new BusinessPayReportRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var first = await service.GenerateReport(request);
        var second = await service.GenerateReport(request);

        Assert.NotNull(first);
        Assert.NotNull(second);
    }

    private PaymentLineItem GeneratePaymentLineItem(
        Clinician? clinician = null,
        decimal paymentAmount = 12.0m,
        decimal adjustmentAmount = 10.0m,
        PaymentAdjustmentCodeEnum code = PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            "Raw clin name",
            paymentAmount,
            adjustmentAmount,
            code,
            DateTime.UtcNow.AddDays(-3),
            "patId",
            "90843",
            Guid.NewGuid().ToString(),
            "payer",
            new EHRUser("test", "test", "test"),
            new ImportBatch("filename", Guid.NewGuid().ToString()),
            10,
            Guid.NewGuid().ToString(),
            DateTime.UtcNow.AddDays(-4),
            DateTime.UtcNow.AddDays(-4));
    }
}
