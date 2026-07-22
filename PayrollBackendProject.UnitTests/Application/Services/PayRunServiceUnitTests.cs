using Moq;
using Xunit;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Domain.Service;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.DTO;

public class PayRunServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPayRunRepository> _payRunRepo = new();
    private readonly Mock<IPayStatementRepository> _payStatementRepo = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();
    private readonly Mock<IUserAccountRepository> _userRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();

    private readonly PayrollCalculator _calculator = new();

    private PayRunService CreateService()
    {
        _payRunRepo
            .Setup(r => r.GetOverlappingPayRuns(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PayRun>());

        return new PayRunService(
            _paymentRepo.Object,
            _unitOfWork.Object,
            _calculator,
            _payRunRepo.Object,
            _payStatementRepo.Object,
            _clinicianRepo.Object,
            _userRepo.Object,
            _auditRepo.Object
        );
    }

    /*
    Validate that executing a pay run creates a new pay run, persists it, 
    and saves changes to the database
    */
    [Fact]
    public async Task ExecutePayRun_ShouldCreatePayRun_AndSaveChanges()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com");

        var payment = GeneratePaymentLineItem(clinician);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO() {StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow};

        var result = await service.ExecutePayRun(request, userId);

        Assert.NotNull(result);
        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Validate that payments without a clinician are filtered out and do not
    cause failures during pay run execution
    */
    [Fact]
    public async Task ExecutePayRun_ShouldFilterOutPaymentsWithoutClinician()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var validClinician = new Clinician("A", "B", "AB@AB.com");

        var validPayment = GeneratePaymentLineItem(validClinician);
        var invalidPayment = GeneratePaymentLineItem(null);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { validPayment, invalidPayment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { validClinician });

        var request = new PayRunRequestDTO() {StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow};

        var result = await service.ExecutePayRun(request, userId);

        Assert.NotNull(result);
    }

    /*
    Validate that retrieving pay statements for a pay run returns correctly
    mapped DTOs from the repository
    */
    [Fact]
    public async Task RetrievePayStatementsForRun_ShouldReturnMappedDTOs()
    {
        var service = CreateService();
        var payRunId = Guid.NewGuid();

        var statements = new List<PayStatement>
        {
            PayStatement.GenerateDraftPayStatement(
                new Clinician("A","B","AB@AB.com"),
                PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow))
        };

        _payStatementRepo
            .Setup(r => r.GetPayStatementsForPayRun(payRunId))
            .ReturnsAsync(statements);

        var result = await service.RetrievePayStatementsForRun(payRunId);

        Assert.Single(result);
    }

    /*
    Validate that approving a pay run throws an exception if the pay run does not exist
    */
    [Fact]
    public async Task ApprovePayRun_ShouldThrow_WhenPayRunNotFound()
    {
        var service = CreateService();

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync((PayRun?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ApprovePayRun(Guid.NewGuid(), Guid.NewGuid()));
    }

    /*
    Validate that approving a pay run throws an exception if the approver does not exist
    */
    [Fact]
    public async Task ApprovePayRun_ShouldThrow_WhenUserNotFound()
    {
        var service = CreateService();

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow));

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync((UserAccount?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ApprovePayRun(Guid.NewGuid(), Guid.NewGuid()));
    }

    /*
    Validate that approving a pay run updates the state, writes an audit log,
    and persists the changes
    */
    [Fact]
    public async Task ApprovePayRun_ShouldApprove_AndSave()
    {
        var service = CreateService();

        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow);
        var user = new UserAccount();

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync(user);
        
        payRun.CalculateTotals();
        await service.ApprovePayRun(Guid.NewGuid(), Guid.NewGuid());

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _auditRepo.Verify(a => a.AddAuditLog(It.IsAny<AuditLog>()), Times.Once);
    }

    /*
    Validate that retrieving statements for a user throws an exception
    if the user does not exist
    */
    [Fact]
    public async Task RetrieveStatementsForUser_ShouldThrow_WhenUserNotFound()
    {
        var service = CreateService();

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync((UserAccount?)null);

        await Assert.ThrowsAsync<Exception>(() =>
            service.RetrieveStatementsForUser(Guid.NewGuid()));
    }

    /*
    Validate that retrieving statements for a user throws an exception
    if the user is not a clinician
    */
    [Fact]
    public async Task RetrieveStatementsForUser_ShouldThrow_WhenNotClinician()
    {
        var service = CreateService();

        var user = new UserAccount
        {
            Role = RoleEnum.ADMIN
        };

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetrieveStatementsForUser(Guid.NewGuid()));
    }

    /*
    Validate that a user whose primary Role is BACKEND (not CLINICIAN) can still
    retrieve statements as long as they have a linked ClinicianId, since Role and
    having clinician data are independent (e.g. a backend staff member who is also
    a practicing clinician).
    */
    [Fact]
    public async Task RetrieveStatementsForUser_ShouldSucceed_WhenBackendUserAlsoHasClinicianId()
    {
        var service = CreateService();

        var clinicianId = Guid.NewGuid();

        var user = new UserAccount
        {
            Role = RoleEnum.BACKEND,
            ClinicianId = clinicianId
        };

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _payStatementRepo.Setup(r => r.GetPayStatementsForUser(clinicianId))
            .ReturnsAsync(new List<PayStatement>());

        var result = await service.RetrieveStatementsForUser(Guid.NewGuid());

        Assert.Empty(result);
    }

    /*
    Validate that retrieving statements for a clinician only returns
    approved pay statements
    */
    [Fact]
    public async Task RetrieveStatementsForUser_ShouldReturnOnlyApproved()
    {
        var service = CreateService();

        var clinicianId = Guid.NewGuid();

        var user = new UserAccount
        {
            Role = RoleEnum.CLINICIAN,
            ClinicianId = clinicianId
        };

        var approver = new UserAccount
        {
            Role = RoleEnum.ADMIN,
        };

        var approved = PayStatement.GenerateDraftPayStatement(
            new Clinician("A","B","AB@AB.com"),
            PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow));
        approved.CalculateTotals();
        approved.Approve(approver);

        var pending = PayStatement.GenerateDraftPayStatement(
            new Clinician("A","B","AB@AB.com"),
            PayRun.GeneratePayRun(DateTime.UtcNow, DateTime.UtcNow));

        _userRepo.Setup(r => r.GetById(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _payStatementRepo.Setup(r => r.GetPayStatementsForUser(clinicianId))
            .ReturnsAsync(new List<PayStatement> { approved, pending });

        var result = await service.RetrieveStatementsForUser(Guid.NewGuid());

        Assert.Single(result);
    }

    /*
    Validate that unapplied code 500 (INSURANCE_TAKEBACK) payments are excluded from the pay run
    and do not affect the statement totals
    */
    [Fact]
    public async Task ExecutePayRun_ShouldExclude_UnappliedCode500Payments()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);

        var normalPayment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);
        var unappliedTakeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { normalPayment, unappliedTakeback });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO { StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow };

        var result = await service.ExecutePayRun(request, userId);

        // Only the normal payment was included; the unapplied code 500 was filtered out
        Assert.Equal(0m, result.TotalCode500Deductions);
        Assert.Equal(500m * 0.6m, result.StatementTotals);
    }

    /*
    Validate that an applied code 500 (INSURANCE_TAKEBACK) payment is included in the pay run
    and its deduction is reflected in the statement's CostShareAdjustedPayment and the
    pay run's TotalCode500Deductions field
    */
    [Fact]
    public async Task ExecutePayRun_WithAppliedCode500_ShouldReduceStatementTotals_AndReportDeductions()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);

        var normalPayment = GeneratePaymentLineItem(clinician, 1000m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);
        var appliedTakeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { normalPayment, appliedTakeback });

        _paymentRepo
            .Setup(r => r.GetPaymentLineItemById(appliedTakeback.Id))
            .ReturnsAsync(appliedTakeback);

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Code500Applications = new List<Code500ApplicationRequestDTO>
            {
                new() { PaymentLineItemId = appliedTakeback.Id, Amount = 200m }
            }
        };

        var result = await service.ExecutePayRun(request, userId);

        // CostShareAdjustedPayment = (1000 - 200) * 0.6 = 480
        Assert.Equal(480m, result.StatementTotals);
        Assert.Equal(200m, result.TotalCode500Deductions);
        Assert.Equal(1000m, result.GrossPaymentTotal);
        Assert.True(appliedTakeback.IsCode500Applied);
    }

    /*
    Validate that applying only part of an outstanding code 500 balance to a pay run deducts
    only that amount, and leaves the remainder outstanding on the line item
    */
    [Fact]
    public async Task ExecutePayRun_WithPartialCode500Application_ShouldApplyOnlyRequestedAmount()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);

        var normalPayment = GeneratePaymentLineItem(clinician, 1000m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);
        var takeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { normalPayment, takeback });

        _paymentRepo
            .Setup(r => r.GetPaymentLineItemById(takeback.Id))
            .ReturnsAsync(takeback);

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Code500Applications = new List<Code500ApplicationRequestDTO>
            {
                new() { PaymentLineItemId = takeback.Id, Amount = 50m }
            }
        };

        var result = await service.ExecutePayRun(request, userId);

        Assert.Equal(50m, result.TotalCode500Deductions);
        Assert.Equal(150m, takeback.RemainingCode500Amount);
        Assert.False(takeback.IsCode500Applied);
    }

    /*
    Validate that requesting more than the remaining outstanding balance for a code 500
    application throws before any pay run is persisted
    */
    [Fact]
    public async Task ExecutePayRun_ShouldThrow_WhenCode500ApplicationExceedsRemainingBalance()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);
        var takeback = GeneratePaymentLineItem(clinician, 0m, -200m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem>());

        _paymentRepo
            .Setup(r => r.GetPaymentLineItemById(takeback.Id))
            .ReturnsAsync(takeback);

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Code500Applications = new List<Code500ApplicationRequestDTO>
            {
                new() { PaymentLineItemId = takeback.Id, Amount = 500m }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecutePayRun(request, userId));
        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Never);
    }

    /*
    Validate that referencing a non-existent payment line item in a code 500 application
    throws KeyNotFoundException
    */
    [Fact]
    public async Task ExecutePayRun_ShouldThrow_WhenCode500LineItemNotFound()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem>());

        _paymentRepo
            .Setup(r => r.GetPaymentLineItemById(It.IsAny<Guid>()))
            .ReturnsAsync((PaymentLineItem?)null);

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician>());

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Code500Applications = new List<Code500ApplicationRequestDTO>
            {
                new() { PaymentLineItemId = Guid.NewGuid(), Amount = 50m }
            }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ExecutePayRun(request, userId));
    }

    /*
    Validate that an eligible clinician (HasPsychToday) receives the flat Psych Today payout
    when the flag is enabled, and that it's reflected in the run-level totals
    */
    [Fact]
    public async Task ExecutePayRun_ShouldAddPsychTodayPayout_ForEligibleClinician()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", true, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = 50m
        };

        var result = await service.ExecutePayRun(request, userId);

        Assert.Equal(50m, result.TotalPsychTodayPayout);
        Assert.Equal(result.StatementTotals + 50m, result.TotalPayout);
    }

    /*
    Validate that a clinician not flagged HasPsychToday does not receive the payout,
    even when the flag is enabled on the request
    */
    [Fact]
    public async Task ExecutePayRun_ShouldNotAddPsychTodayPayout_WhenClinicianNotFlagged()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", false, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = 50m
        };

        var result = await service.ExecutePayRun(request, userId);

        Assert.Equal(0m, result.TotalPsychTodayPayout);
        Assert.Equal(result.StatementTotals, result.TotalPayout);
    }

    /*
    Validate that an eligible clinician does not receive the payout when the flag is disabled
    */
    [Fact]
    public async Task ExecutePayRun_ShouldNotAddPsychTodayPayout_WhenFlagDisabled()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com", true, 0.6);
        var payment = GeneratePaymentLineItem(clinician, 500m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow
        };

        var result = await service.ExecutePayRun(request, userId);

        Assert.Equal(0m, result.TotalPsychTodayPayout);
    }

    /*
    Validate that a pay run date range overlapping an existing pay run is rejected before
    any payments are gathered or a new pay run is persisted
    */
    [Fact]
    public async Task ExecutePayRun_ShouldThrow_WhenDateRangeOverlapsExistingPayRun()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var existingPayRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3));

        _payRunRepo
            .Setup(r => r.GetOverlappingPayRuns(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PayRun> { existingPayRun });

        var request = new PayRunRequestDTO { StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecutePayRun(request, userId));

        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Never);
        _paymentRepo.Verify(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    /*
    Validate that an unresolved payment whose raw provider is the "!SELECT PROVIDER" sentinel
    blocks the entire pay run before anything is persisted
    */
    [Theory]
    [InlineData("!SELECT PROVIDER")]
    [InlineData("!select provider")]
    [InlineData(" !Select Provider ")]
    public async Task ExecutePayRun_ShouldThrow_WhenUnresolvedSentinelPaymentExists(string rawClinicianName)
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var sentinelPayment = GeneratePaymentLineItem(null, rawClinicianName: rawClinicianName);

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { sentinelPayment });

        var request = new PayRunRequestDTO { StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecutePayRun(request, userId));

        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /*
    Validate that a payment whose raw provider was the sentinel but has since been manually
    assigned a clinician no longer blocks the pay run
    */
    [Fact]
    public async Task ExecutePayRun_ShouldSucceed_WhenSentinelPaymentAlreadyAssignedClinician()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var clinician = new Clinician("A", "B", "AB@AB.com");
        var resolvedSentinelPayment = GeneratePaymentLineItem(clinician, rawClinicianName: "!SELECT PROVIDER");

        _paymentRepo
            .Setup(r => r.GetPaymentBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PaymentLineItem> { resolvedSentinelPayment });

        _clinicianRepo
            .Setup(r => r.GetAllClinicians())
            .ReturnsAsync(new List<Clinician> { clinician });

        var request = new PayRunRequestDTO { StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow };

        var result = await service.ExecutePayRun(request, userId);

        Assert.NotNull(result);
        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Once);
    }

    /*
    Validate that enabling the flag without a positive amount throws before any pay run is created
    */
    [Fact]
    public async Task ExecutePayRun_ShouldThrow_WhenPsychTodayPayoutEnabledWithoutAmount()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var request = new PayRunRequestDTO
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            IncludePsychTodayPayout = true,
            PsychTodayPayoutAmount = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecutePayRun(request, userId));

        _payRunRepo.Verify(r => r.AddPayRun(It.IsAny<PayRun>()), Times.Never);
    }

    private PaymentLineItem GeneratePaymentLineItem(
        Clinician? clinician = null,
        decimal paymentAmount = 12.0m,
        decimal adjustmentAmount = 10.0m,
        PaymentAdjustmentCodeEnum code = PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
        string rawClinicianName = "Raw clin name")
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            rawClinicianName,
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