using Xunit;
using Moq;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class CsvParserServiceUnitTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();
    private readonly Mock<IUserAccountRepository> _userRepo = new();
    private readonly Mock<IEHRUserAccountRepository> _ehrRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFingerprintGenerator> _fingerprint = new();

    private CsvParserService CreateService()
    {
        return new CsvParserService(
            _paymentRepo.Object,
            _clinicianRepo.Object,
            _userRepo.Object,
            _ehrRepo.Object,
            _unitOfWork.Object,
            _fingerprint.Object
        );
    }

    private PaymentLineItem GeneratePaymentLineItem(string rawClinicianName = "name")
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            rawData: "Raw",
            clinician: null,
            rawClinicianName: rawClinicianName,
            paymentAmount: 10.0m,
            adjustmentAmount: 15.0m,
            adjustmentCode: PaymentAdjustmentCodeEnum.INSURANCE_ADJUSTMENT,
            dateOfService: DateTime.UtcNow,
            patientId: "patId",
            cptCode: "cpt",
            paymentId: "payId",
            payer: "payer",
            appliedBy: new EHRUser(),
            importBatch: new ImportBatch("filename", "Fingerprint"),
            rowNumber: 10,
            fingerprint: "fingerprint",
            appliedDate: DateTime.UtcNow,
            paymentDate: DateTime.UtcNow
        );
    }

    /*
    Test that MatchUnresolvedPaymentLineItem returns false when clinician cannot be found
    */
    [Fact]
    public void MatchUnresolvedPaymentLineItem_NoMatch_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var clinicians = new List<Clinician>();

        var lineItem = GeneratePaymentLineItem("John Doe");

        // Act
        var result = InvokePrivateMatch(service, lineItem, clinicians);

        // Assert
        Assert.False(result);
    }

    /*
    Test that MatchUnresolvedPaymentLineItem updates clinician when match is found
    */
    [Fact]
    public void MatchUnresolvedPaymentLineItem_MatchFound_UpdatesClinician()
    {
        // Arrange
        var service = CreateService();
        var clinician = new Clinician("John", "Doe", "jd@test.com");
        var clinicians = new List<Clinician> { clinician };

        var lineItem = GeneratePaymentLineItem("John Doe");

        // Act
        var result = InvokePrivateMatch(service, lineItem, clinicians);

        // Assert
        Assert.True(result);
    }

    /*
    Test that MatchUnresolvedPaymentLineItems resolves matching payments
    */
    [Fact]
    public async Task MatchUnresolvedPaymentLineItems_ResolvesMatches()
    {
        // Arrange
        var service = CreateService();

        var clinician = new Clinician("Jane", "Smith", "js@test.com");
        var clinicians = new List<Clinician> { clinician };

        var payment = GeneratePaymentLineItem("Jane Smith");

        _paymentRepo.Setup(x => x.GetPaymentsWithUnresolvedClinician())
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo.Setup(x => x.GetAllClinicians())
            .ReturnsAsync(clinicians);

        // Act
        var result = await service.MatchUnresolvedPaymentLineItems();

        // Assert
        Assert.Equal(1, result.ResolvedRows);
        Assert.Equal(0, result.FailedRows);
    }

    /*
    Test that MatchUnresolvedPaymentLineItems tracks failures when no match found
    */
    [Fact]
    public async Task MatchUnresolvedPaymentLineItems_NoMatch_AddsError()
    {
        // Arrange
        var service = CreateService();

        var payment = GeneratePaymentLineItem("Unknown Person");

        _paymentRepo.Setup(x => x.GetPaymentsWithUnresolvedClinician())
            .ReturnsAsync(new List<PaymentLineItem> { payment });

        _clinicianRepo.Setup(x => x.GetAllClinicians())
            .ReturnsAsync(new List<Clinician>());

        // Act
        var result = await service.MatchUnresolvedPaymentLineItems();

        // Assert
        Assert.Equal(0, result.ResolvedRows);
        Assert.Equal(1, result.FailedRows);
        Assert.Single(result.Errors);
    }

    /*
    Test that FindAppliedByUser returns existing user when found
    */
    [Fact]
    public void FindAppliedByUser_UserExists_ReturnsExisting()
    {
        // Arrange
        var service = CreateService();
        var users = new List<EHRUser>
        {
            new EHRUser { EHRUsername = "user1" }
        };

        // Act
        var result = InvokePrivateFindUser(service, "user1", users);

        // Assert
        Assert.Equal("user1", result.EHRUsername);
        _ehrRepo.Verify(x => x.AddNewUser(It.IsAny<EHRUser>()), Times.Never);
    }

    /*
    Test that FindAppliedByUser creates new user when not found
    */
    [Fact]
    public void FindAppliedByUser_UserDoesNotExist_CreatesNew()
    {
        // Arrange
        var service = CreateService();
        var users = new List<EHRUser>();

        // Act
        var result = InvokePrivateFindUser(service, "newUser", users);

        // Assert
        Assert.Equal("newUser", result.EHRUsername);
        _ehrRepo.Verify(x => x.AddNewUser(It.IsAny<EHRUser>()), Times.Once);
    }

    // Reflection helpers
    private static bool InvokePrivateMatch(CsvParserService service, PaymentLineItem item, List<Clinician> clinicians)
    {
        var method = typeof(CsvParserService)
            .GetMethod("MatchUnresolvedPaymentLineItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (bool)method.Invoke(service, new object[] { item, clinicians })!;
    }

    private static EHRUser InvokePrivateFindUser(CsvParserService service, string username, List<EHRUser> users)
    {
        var method = typeof(CsvParserService)
            .GetMethod("FindAppliedByUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (EHRUser)method.Invoke(service, new object[] { username, users })!;
    }
}