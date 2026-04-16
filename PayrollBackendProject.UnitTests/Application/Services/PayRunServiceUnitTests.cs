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

    private PaymentLineItem GeneratePaymentLineItem(Clinician? clinician = null)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            "Raw clin name",
            12.0m,
            10.0m,
            PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            DateTime.UtcNow.AddDays(-3),
            "patId",
            "90843",
            "paymentId",
            "payer",
            new EHRUser("test", "test", "test"),
            new ImportBatch("filename", "UniqueFingerPrint"),
            10,
            "fingerprint",
            DateTime.UtcNow.AddDays(-4),
            DateTime.UtcNow.AddDays(-4));
    }
}