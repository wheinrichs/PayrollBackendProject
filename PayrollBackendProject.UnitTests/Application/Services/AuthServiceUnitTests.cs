using Moq;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Interfaces.Services;


namespace PayrollBackendProject.UnitTests;

public class AuthServiceUnitTests
{
    [Fact]
    public async Task Login_ShouldReturnNull_WhenUserDoesNotExist()
    {
        Mock<IUserAccountRepository> mockRepo = new();
        mockRepo.Setup(r => r.GetByEmail("test@test.com")).ReturnsAsync((UserAccount?)null);

        var service = CreateService(mockRepo: mockRepo);

        var result = await service.Login("test@test.com", "password");

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ShouldReturnUser_WhenUserExists()
    {
        Mock<IUserAccountRepository> mockRepo = new();
        string email = "test@test.com";
        string password = "testPassword";

        UserAccount retrievedUser = UserAccount.GenerateUserAccount(email, password, "test", "test", RoleEnum.ADMIN, null);
        retrievedUser.PasswordHash = "hash";
        mockRepo.Setup(r => r.GetByEmail(email)).ReturnsAsync(retrievedUser);

        var mockHasher = new Mock<IPasswordHasher>();
        mockHasher.Setup(h => h.Verify(password, "hash")).Returns(true);

        var mockToken = new Mock<ITokenService>();
        mockToken.Setup(t => t.GenerateToken(retrievedUser)).Returns("token");

        AuthService service = CreateService(mockRepo, mockToken, passwordHasher: mockHasher);
        var result = await service.Login(email, password);

        Assert.NotNull(result);
        Assert.Equal("token", result.Token);
    }

    /*
    Return null if the password does not match the found user.
    */
    [Fact]
    public async Task Login_ShouldReturnNull_WhenPasswordInvalid()
    {
        string email = "test@test.com";
        string password = "testPassword";
        UserAccount retrievedUser = UserAccount.GenerateUserAccount(email, password, "test", "test", RoleEnum.ADMIN, null);
        retrievedUser.PasswordHash = "hash";

        Mock<IUserAccountRepository> userRepo = new();
        userRepo.Setup(r => r.GetByEmail(email)).ReturnsAsync(retrievedUser);

        Mock<IPasswordHasher> passwordHasher = new();
        passwordHasher.Setup(r => r.Verify(password, "HashNotTheSame")).Returns(false);

        AuthService service = CreateService(userRepo, passwordHasher: passwordHasher);
        var result = await service.Login(email, password);
        Assert.Null(result);
    }

    /*
    Signup should return null if the user exists
    */
    [Fact]
    public async Task SignUp_ShouldReturnNull_WhenUserAlreadyExists()
    {
        Mock<IUserAccountRepository> userRepo = new();
        userRepo.Setup(r => r.GetByEmail(It.IsAny<string>())).ReturnsAsync(new UserAccount());

        var service = CreateService(userRepo);
        var result = await service.SignUp(new SignUpRequestDTO("test@test.com", "password", "", "", ""), RoleEnum.BACKEND);
        Assert.Null(result);
    }

    /*
    Signup should return a user if one did not already exist
    */
    [Fact]
    public async Task SignUp_ShouldReturnNewUser_WhenUserDoesntExist()
    {
        // Create a new user if one did not already exist
        string email = "test@test.com";
        string password = "testPassword";
        UserAccount retrievedUser = UserAccount.GenerateUserAccount(email, password, "test", "test", RoleEnum.ADMIN, null);

        // Create mocks
        Mock<IUserAccountRepository> userRepo = new();
        Mock<IPasswordHasher> passwordHasher = new();
        Mock<ITokenService> tokenService = new();

        userRepo.Setup(r => r.GetByEmail(It.IsAny<string>())).ReturnsAsync((UserAccount?)null);
        passwordHasher.Setup(r => r.Hash(password)).Returns("HashedPassword");
        tokenService.Setup(r => r.GenerateToken(It.IsAny<UserAccount>())).Returns("NewToken");

        var service = CreateService(userRepo, tokenService, passwordHasher: passwordHasher);

        var returnedUser = await service.SignUp(new SignUpRequestDTO(email, password, "test", "test", "Admin"), RoleEnum.ADMIN);

        Assert.Equal("NewToken", returnedUser!.Token);
        userRepo.Verify(r => r.SignUp(It.IsAny<UserAccount>()), Times.Once);
    }

    /*
    Signup should create a new clinician if the role type is clinician and one did not exist
    */
    [Fact]
    public async Task SignUp_ShouldReturnNewClinician_WhenClinicianDoesntExist()
    {
        var mockRepo = new Mock<IUserAccountRepository>();
        mockRepo.Setup(r => r.GetByEmail(It.IsAny<string>()))
                .ReturnsAsync((UserAccount?)null);

        var mockClinicianRepo = new Mock<IClinicianRepository>();
        mockClinicianRepo.Setup(c => c.GetClinicianByEmail(It.IsAny<string>()))
                        .ReturnsAsync((Clinician?)null);

        var service = CreateService(mockRepo, clinicianRepo: mockClinicianRepo);

        var dto = new SignUpRequestDTO("test@test.com", "password", "A", "B", "Clinician");

        await service.SignUp(dto, RoleEnum.CLINICIAN);

        mockClinicianRepo.Verify(c => c.AddClinician(It.IsAny<Clinician>()), Times.Once);
    }

    /*
    Signup should use the existing clincian if the user type is clincian and one already existed
    */
    [Fact]
    public async Task SignUp_ShouldUseExistingClinician_WhenExists()
    {
        var mockRepo = new Mock<IUserAccountRepository>();
        mockRepo.Setup(r => r.GetByEmail(It.IsAny<string>()))
                .ReturnsAsync((UserAccount?)null);

        var mockClinicianRepo = new Mock<IClinicianRepository>();
        mockClinicianRepo.Setup(c => c.GetClinicianByEmail(It.IsAny<string>()))
                        .ReturnsAsync(new Clinician("A", "B", "test@test.com"));

        var service = CreateService(mockRepo, clinicianRepo: mockClinicianRepo);

        var dto = new SignUpRequestDTO("test@test.com", "password", "A", "B", "Clinician");

        await service.SignUp(dto, RoleEnum.CLINICIAN);

        mockClinicianRepo.Verify(c => c.AddClinician(It.IsAny<Clinician>()), Times.Never);
    }


    private AuthService CreateService(
        Mock<IUserAccountRepository>? mockRepo = null,
        Mock<ITokenService>? mockToken = null,
        Mock<IUnitOfWork>? mockUow = null,
        Mock<IClinicianRepository>? clinicianRepo = null,
        Mock<IPasswordHasher>? passwordHasher = null)
    {
        return new AuthService(
            (mockRepo ?? new Mock<IUserAccountRepository>()).Object,
            (mockToken ?? new Mock<ITokenService>()).Object,
            (mockUow ?? new Mock<IUnitOfWork>()).Object,
            (clinicianRepo ?? new Mock<IClinicianRepository>()).Object,
            (passwordHasher ?? new Mock<IPasswordHasher>()).Object
        );
    }
}

