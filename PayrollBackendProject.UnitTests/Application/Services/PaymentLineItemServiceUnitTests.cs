using Moq;
using Xunit;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

public class PaymentLineItemServiceUnitTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();
    private readonly Mock<IEHRUserAccountRepository> _ehrUserRepo = new();
    private readonly Mock<IUserAccountRepository> _userAccountRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepo = new();
    private readonly Mock<IFingerprintGenerator> _fingerprintGenerator = new();

    private PaymentLineItemService CreateService() => new(
        _paymentRepo.Object,
        _clinicianRepo.Object,
        _ehrUserRepo.Object,
        _userAccountRepo.Object,
        _unitOfWork.Object,
        _auditLogRepo.Object,
        _fingerprintGenerator.Object
    );

    private ManualPaymentRequestDTO BuildValidDto(Guid? clinicianId = null) => new()
    {
        PaymentAmount = 100m,
        AdjustmentAmount = 0m,
        PaymentAdjustmentCode = (int)PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT,
        DateOfService = DateTime.UtcNow.AddDays(-10),
        PatientId = "P001",
        CPTCode = "90837",
        PaymentId = "PAY-001",
        Payer = "CIGNA",
        RawClinicianName = "Jane Smith",
        ClinicianId = clinicianId,
        AppliedDate = DateTime.UtcNow,
        PaymentDate = DateTime.UtcNow
    };

    private void SetupHappyPathUserResolution(Guid userId, string email = "user@test.com")
    {
        var userAccount = new UserAccount(email, "hash", "First", "Last") { Id = userId };
        _userAccountRepo.Setup(r => r.GetById(userId)).ReturnsAsync(userAccount);
        _ehrUserRepo.Setup(r => r.GetUserByUsername(email)).ReturnsAsync(new EHRUser("First", "Last", email));
    }

    /*
    Validate that AddManualPayment creates a payment, logs the creation, saves changes,
    and returns the new payment's ID
    */
    [Fact]
    public async Task AddManualPayment_ShouldCreatePayment_AndReturnId()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var dto = BuildValidDto();
        var fingerprint = "fp-unique-001";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(dto.PatientId, dto.CPTCode, dto.PaymentId!, dto.DateOfService, dto.PaymentAdjustmentCode))
            .ReturnsAsync(fingerprint);
        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync((PaymentLineItem?)null);
        SetupHappyPathUserResolution(userId);

        var result = await service.AddManualPayment(dto, userId);

        Assert.NotEqual(Guid.Empty, result);
        _paymentRepo.Verify(r => r.AddLineItem(It.IsAny<PaymentLineItem>()), Times.Once);
        _auditLogRepo.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Validate that AddManualPayment looks up and attaches the clinician when ClinicianId is provided
    */
    [Fact]
    public async Task AddManualPayment_WithClinicianId_ShouldLookUpClinician()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var clinician = new Clinician("Jane", "Smith", "jane@clinic.com");
        var dto = BuildValidDto(clinicianId: clinician.ID);
        var fingerprint = "fp-with-clinician";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(fingerprint);
        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync((PaymentLineItem?)null);
        _clinicianRepo.Setup(r => r.GetClinicianByID(clinician.ID)).ReturnsAsync(clinician);
        SetupHappyPathUserResolution(userId);

        var result = await service.AddManualPayment(dto, userId);

        Assert.NotEqual(Guid.Empty, result);
        _clinicianRepo.Verify(r => r.GetClinicianByID(clinician.ID), Times.Once);
        _paymentRepo.Verify(r => r.AddLineItem(It.Is<PaymentLineItem>(p => p.ClinicianId == clinician.ID)), Times.Once);
    }

    /*
    Validate that a new EHR user is created when a UserAccount exists but has no matching EHR user
    */
    [Fact]
    public async Task AddManualPayment_WhenNoEHRUserExists_ShouldCreateOne()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var dto = BuildValidDto();
        var fingerprint = "fp-new-ehr-user";
        var email = "newehr@test.com";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(fingerprint);
        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync((PaymentLineItem?)null);

        var userAccount = new UserAccount(email, "hash", "New", "User") { Id = userId };
        _userAccountRepo.Setup(r => r.GetById(userId)).ReturnsAsync(userAccount);
        _ehrUserRepo.Setup(r => r.GetUserByUsername(email)).ReturnsAsync((EHRUser?)null);

        await service.AddManualPayment(dto, userId);

        _ehrUserRepo.Verify(r => r.AddNewUser(It.Is<EHRUser>(u => u.EHRUsername == email)), Times.Once);
    }

    /*
    Validate that AddManualPayment throws ArgumentException when the adjustment code is not a valid enum value
    */
    [Fact]
    public async Task AddManualPayment_WithInvalidAdjustmentCode_ShouldThrowArgumentException()
    {
        var service = CreateService();
        var dto = BuildValidDto();
        dto.PaymentAdjustmentCode = 9999;

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddManualPayment(dto, Guid.NewGuid()));
    }

    /*
    Validate that AddManualPayment throws InvalidOperationException when a payment with the same
    fingerprint already exists, preventing duplicate entries
    */
    [Fact]
    public async Task AddManualPayment_WhenDuplicateFingerprint_ShouldThrowInvalidOperationException()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var dto = BuildValidDto();
        var fingerprint = "fp-duplicate";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(fingerprint);

        var existingUser = new EHRUser("A", "B", "a@b.com");
        var existingImportBatch = new ImportBatch("file.csv", "fp-batch");
        var existingPayment = PaymentLineItem.GeneratePaymentLineItem(
            "raw", null, "Some Clinician", 50m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT,
            DateTime.UtcNow, "P001", "90837", "PAY-001", "CIGNA",
            existingUser, existingImportBatch, 1, fingerprint, DateTime.UtcNow, null);

        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync(existingPayment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddManualPayment(dto, userId));
    }

    /*
    Validate that AddManualPayment throws KeyNotFoundException when ClinicianId is supplied
    but no matching clinician is found in the repository
    */
    [Fact]
    public async Task AddManualPayment_WhenClinicianNotFound_ShouldThrowKeyNotFoundException()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var missingClinicianId = Guid.NewGuid();
        var dto = BuildValidDto(clinicianId: missingClinicianId);
        var fingerprint = "fp-missing-clinician";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(fingerprint);
        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync((PaymentLineItem?)null);
        _clinicianRepo.Setup(r => r.GetClinicianByID(missingClinicianId)).ReturnsAsync((Clinician)null!);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddManualPayment(dto, userId));
    }

    /*
    Validate that AddManualPayment throws KeyNotFoundException when the userId does not
    correspond to an existing UserAccount
    */
    [Fact]
    public async Task AddManualPayment_WhenUserAccountNotFound_ShouldThrowKeyNotFoundException()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var dto = BuildValidDto();
        var fingerprint = "fp-missing-user";

        _fingerprintGenerator
            .Setup(f => f.ManualEntryComputeSHA256Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(fingerprint);
        _paymentRepo.Setup(r => r.GetPaymentLineItem(fingerprint)).ReturnsAsync((PaymentLineItem?)null);
        _userAccountRepo.Setup(r => r.GetById(userId)).ReturnsAsync((UserAccount?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddManualPayment(dto, userId));
    }

    /*
    Validate that GetUnappliedCode500Payments returns a DTO for each unapplied takeback payment
    returned by the repository
    */
    [Fact]
    public async Task GetUnappliedCode500Payments_ShouldReturnMappedList()
    {
        var service = CreateService();

        var ehrUser = new EHRUser("A", "B", "a@b.com");
        var batch = new ImportBatch("file.csv", "fp-takeback");
        var payment = PaymentLineItem.GeneratePaymentLineItem(
            "raw", null, "Dr. Smith", 0m, -150m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK,
            DateTime.UtcNow.AddDays(-10), "P999", "90837", "PAY-TB-01", "CIGNA",
            ehrUser, batch, 1, "fp-takeback-unique", DateTime.UtcNow.AddDays(-5), null);

        _paymentRepo.Setup(r => r.GetUnappliedCode500Payments())
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        var result = await service.GetUnappliedCode500Payments();

        Assert.Single(result);
        Assert.Equal(payment.Id, result[0].Id);
        Assert.Equal(-150m, result[0].AdjustmentAmount);
        Assert.Equal(0m, result[0].AppliedAmount);
        Assert.Equal(150m, result[0].RemainingAmount);
    }

    /*
    Validate that RejectCode500Payment rejects the item, logs the action, and saves changes
    */
    [Fact]
    public async Task RejectCode500Payment_ShouldRejectItem_AndLogAndSave()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var ehrUser = new EHRUser("A", "B", "a@b.com");
        var batch = new ImportBatch("file.csv", "fp-reject-batch");
        var payment = PaymentLineItem.GeneratePaymentLineItem(
            "raw", null, "Dr. Smith", 0m, -150m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK,
            DateTime.UtcNow.AddDays(-10), "P999", "90837", "PAY-TB-01", "CIGNA",
            ehrUser, batch, 1, "fp-reject-unique", DateTime.UtcNow.AddDays(-5), null);

        _paymentRepo.Setup(r => r.GetPaymentLineItemById(payment.Id)).ReturnsAsync(payment);

        await service.RejectCode500Payment(payment.Id, userId);

        Assert.True(payment.IsRejected);
        Assert.Equal(userId, payment.RejectedById);
        _auditLogRepo.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Validate that RejectCode500Payment throws KeyNotFoundException when the item does not exist
    */
    [Fact]
    public async Task RejectCode500Payment_WhenNotFound_ShouldThrowKeyNotFoundException()
    {
        var service = CreateService();
        var id = Guid.NewGuid();

        _paymentRepo.Setup(r => r.GetPaymentLineItemById(id)).ReturnsAsync((PaymentLineItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RejectCode500Payment(id, Guid.NewGuid()));
    }

    /*
    Validate that RejectCode500Payment surfaces the domain's InvalidOperationException
    when the item already has an applied amount
    */
    [Fact]
    public async Task RejectCode500Payment_WhenAlreadyApplied_ShouldThrowInvalidOperationException()
    {
        var service = CreateService();
        var ehrUser = new EHRUser("A", "B", "a@b.com");
        var batch = new ImportBatch("file.csv", "fp-reject-applied-batch");
        var payment = PaymentLineItem.GeneratePaymentLineItem(
            "raw", null, "Dr. Smith", 0m, -150m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK,
            DateTime.UtcNow.AddDays(-10), "P999", "90837", "PAY-TB-02", "CIGNA",
            ehrUser, batch, 1, "fp-reject-applied-unique", DateTime.UtcNow.AddDays(-5), null);
        var clinician = new Clinician("Jane", "Smith", "jane@clinic.com");
        payment.UpdateClinician(clinician);
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddDays(-1));
        payment.ApplyCode500(50m, Guid.NewGuid(), payRun);

        _paymentRepo.Setup(r => r.GetPaymentLineItemById(payment.Id)).ReturnsAsync(payment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectCode500Payment(payment.Id, Guid.NewGuid()));
    }
}
