using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class ClinicianUnitTests
{
    /*
    Test that passing in valid arguments for the Clinician main constructor works
    */
    [Fact]
    public void Constructor_ShouldSetProvidedValues()
    {
        string firstName = "Winston";
        string lastName = "Heinrichs";
        string email = "winston.heinrichs@example.com";
        bool psychToday = true;
        double costShare = 0.8;

        Clinician testClinician = new(firstName, lastName, email, psychToday, costShare);

        Assert.Equal(firstName, testClinician.FirstName);
        Assert.Equal(lastName, testClinician.LastName);
        Assert.Equal(email, testClinician.Email);
        Assert.Equal(psychToday, testClinician.HasPsychToday);
        Assert.Equal(costShare, testClinician.CostShare);
    }

    /*
    Test that passing in empty values for firstName does not work
    Test that passing in empty values for lastName does not work
    Test that passing in empty values for email does not work
    Test that passing in an invalid email froms an exception
    */
    [Theory]
    [InlineData("", "Heinrichs", "Winston.Heinrichs@example.com")]
    [InlineData("Winston", "", "Winston.Heinrichs@example.com")]
    [InlineData("Winston", "Heinrichs", "")]
    [InlineData("Winston", "Heinrichs", "Winston.Heinrichsexample.com")]
    public void Constructor_ShouldThrowArgumentException_WhenParametersEmpty(
        string firstName,
        string lastName,
        string email
    )
    {
        bool psychToday = true;
        double costShare = 0.8;

        Assert.Throws<ArgumentException>(() => new Clinician(firstName, lastName, email, psychToday, costShare));
    }

    /*
    Test that passing in a cost share above 1 does not work
    Test that passing in a cost share below 0 does not work
    */
    [Theory]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void Constructor_ThrowsInvalidArgumentException_WhenCostShareInvalid(double costShare)
    {
        string firstName = "Winston";
        string lastName = "Heinrichs";
        string email = "winston.heinrichs@example.com";
        bool psychToday = true;

        Assert.Throws<ArgumentException>(() => new Clinician(firstName, lastName, email, psychToday, costShare));
    }

    /*
    Test passing valid inputs to the secondary constructor
    */
    [Fact]
    public void SecondaryConstructor_ShouldSetProvidedValues()
    {
        string firstName = "Winston";
        string lastName = "Heinrichs";
        string email = "winston.heinrichs@example.com";


        Clinician testClinician = new(firstName, lastName, email);

        Assert.Equal(firstName, testClinician.FirstName);
        Assert.Equal(lastName, testClinician.LastName);
        Assert.Equal(email, testClinician.Email);
    }

    /*
    Test that passing in empty values for firstName does not work in the secondary constructor 
    Test that passing in empty values for lastName does not work in the secondary constructor 
    Test that passing in empty values for email does not work in the secondary constructor 
    */
    [Theory]
    [InlineData("", "Heinrichs", "Winston.Heinrichs@example.com")]
    [InlineData("Winston", "", "Winston.Heinrichs@example.com")]
    [InlineData("Winston", "Heinrichs", "")]
    [InlineData("Winston", "Heinrichs", "Winston.Heinrichsexample.com")]
    public void SecondaryConstructor_ShouldThrowArgumentException_WhenParametersEmpty(
        string firstName,
        string lastName,
        string email
    )
    {
        Assert.Throws<ArgumentException>(() => new Clinician(firstName, lastName, email));
    }

    /* 
    Test that updating core information works with valid input
    */
    [Fact]
    public void UpdateCoreInformation_ShouldSetProvidedValues()
    {
        string firstName = "Winston";
        string lastName = "Heinrichs";
        string email = "winston.heinrichs@example.com";
        bool psychToday = true;
        double costShare = 0.8;

        Clinician testClinician = new(firstName, lastName, email, psychToday, costShare);
        testClinician.UpdateCoreInformation("Alexis", "Kremp", true, 0.9);

        Assert.Equal("Alexis", testClinician.FirstName);
        Assert.Equal("Kremp", testClinician.LastName);
        Assert.Equal(email, testClinician.Email);
        Assert.True(testClinician.HasPsychToday);
        Assert.Equal(0.9, testClinician.CostShare);
    }

    /*
    Test that passing in empty values for firstName does not work in updating core information
    Test that passing in empty values for lastName does not work in updating core information
    Test that passing in a cost share above 1 does not work in updating core information
    Test that passing in a cost share below 0 does not work in updating core information
    */
    [Theory]
    [InlineData("", "Heinrichs", 0.9)]
    [InlineData("Winston", "", 0.9)]
    [InlineData("Winston", "Heinrichs", 1.1)]
    [InlineData("Winston", "Heinrichs", -1)]
    public void UpdateCoreInformation_ShouldThrowArgumentException_WhenParametersEmpty(
        string firstName,
        string lastName,
        double costShare
    )
    {
        string firstNameOriignal = "Alexis";
        string lastNameOriginal = "Kremp";
        string email = "alexis.kremp@example.com";
        bool psychToday = true;
        double costShareOriginal = 0.8;

        Clinician testClinician = new(firstNameOriignal, lastNameOriginal, email, psychToday, costShareOriginal);
        Assert.Throws<ArgumentException>(() => testClinician.UpdateCoreInformation(firstName, lastName, true, costShare));
    }
}