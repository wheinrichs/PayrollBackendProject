using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class UserAccountUnitTests
{
    private static Clinician GetClinician()
        => new Clinician("John", "Doe", "john@test.com", false, 0.5);

    /*
    Test constructor sets provided values correctly
    */
    [Theory]
    [InlineData("test@test.com", "password", "John", "Doe")]
    [InlineData("", "", "", "")]
    public void Constructor_ShouldSetValues(string email, string password, string firstName, string lastName)
    {
        var user = new UserAccount(email, password, firstName, lastName);

        Assert.Equal(email, user.Email);
        Assert.Equal(password, user.PasswordHash);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    /*
    Test GenerateUserAccount sets role and basic fields correctly
    */
    [Theory]
    [InlineData(RoleEnum.ADMIN)]
    [InlineData(RoleEnum.BACKEND)]
    public void GenerateUserAccount_ShouldSetRole_ForNonClinician(RoleEnum role)
    {
        var user = UserAccount.GenerateUserAccount(
            "test@test.com",
            "password",
            "John",
            "Doe",
            role,
            null
        );

        Assert.Equal(role, user.Role);
        Assert.Null(user.Clinician);
        Assert.Null(user.ClinicianId);
    }

    /*
    Test GenerateUserAccount assigns clinician when role is CLINICIAN
    */
    [Fact]
    public void GenerateUserAccount_ShouldAssignClinician_WhenRoleClinician()
    {
        var clinician = GetClinician();

        var user = UserAccount.GenerateUserAccount(
            "test@test.com",
            "password",
            "John",
            "Doe",
            RoleEnum.CLINICIAN,
            clinician
        );

        Assert.Equal(RoleEnum.CLINICIAN, user.Role);
        Assert.Equal(clinician, user.Clinician);
        Assert.Equal(clinician.ID, user.ClinicianId);
    }

    /*
    Test GenerateUserAccount throws when clinician role but no clinician provided
    */
    [Fact]
    public void GenerateUserAccount_ShouldThrow_WhenClinicianRoleWithoutClinician()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserAccount.GenerateUserAccount(
                "test@test.com",
                "password",
                "John",
                "Doe",
                RoleEnum.CLINICIAN,
                null
            )
        );
    }

    /*
    Test GenerateUserAccount sets all base properties correctly
    */
    [Fact]
    public void GenerateUserAccount_ShouldSetBaseFields()
    {
        var user = UserAccount.GenerateUserAccount(
            "email@test.com",
            "password123",
            "Jane",
            "Smith",
            RoleEnum.ADMIN,
            null
        );

        Assert.Equal("email@test.com", user.Email);
        Assert.Equal("password123", user.PasswordHash);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.NotEqual(Guid.Empty, user.Id);
    }
}