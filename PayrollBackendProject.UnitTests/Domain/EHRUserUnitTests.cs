using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.UnitTests;

public class EHRUserUnitTests
{
    /*
    Test that constructor sets provided values correctly
    Test that empty strings are handled correctly
    */
    [Theory]
    [InlineData("Winston", "Heinrichs", "wheinrichs")]
    [InlineData("", "", "")]
    [InlineData("Alice", "Smith", "asmith123")]
    public void Constructor_ShouldSetProvidedValues(
        string firstName,
        string lastName,
        string ehrUsername
    )
    {
        var user = new EHRUser(firstName, lastName, ehrUsername);

        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(ehrUsername, user.EHRUsername);
    }

    /*
    Test that a new Guid is assigned on construction
    */
    [Fact]
    public void Constructor_ShouldAssignValidGuid()
    {
        var user = new EHRUser("John", "Doe", "jdoe");

        Assert.NotEqual(Guid.Empty, user.Id);
    }

    /*
    Test that navigation properties are null by default
    */
    [Fact]
    public void Constructor_ShouldInitializeNavigationPropertiesAsNull()
    {
        var user = new EHRUser("John", "Doe", "jdoe");

        Assert.Null(user.Clinician);
        Assert.Null(user.UserAccount);
    }

    /*
    Test that default constructor initializes properties correctly
    */
    [Fact]
    public void DefaultConstructor_ShouldInitializeDefaults()
    {
        var user = new EHRUser();

        Assert.Equal(Guid.Empty, user.Id); // not set
        Assert.Equal(string.Empty, user.FirstName);
        Assert.Equal(string.Empty, user.LastName);
        Assert.Equal(string.Empty, user.EHRUsername);
        Assert.Null(user.Clinician);
        Assert.Null(user.UserAccount);
    }
}