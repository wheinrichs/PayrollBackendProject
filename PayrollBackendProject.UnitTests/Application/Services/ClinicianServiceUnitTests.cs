using Moq;
using Xunit;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.UnitTests;

public class ClinicianServiceUnitTests
{
    private readonly Mock<IClinicianRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ClinicianService CreateService()
    {
        return new ClinicianService(_repo.Object, _unitOfWork.Object);
    }

    /*
    Test adding a clinician before a user account exists creates a new clinician
    */
    [Fact]
    public async Task AddClinician_ShouldCreateNewClinician_WhenClinicianDoesNotExist()
    {
        var service = CreateService();

        var request = new ClinicianRequestDTO("A", "B", "test@test.com", true, 0.5);

        _repo.Setup(r => r.GetClinicianByEmail(request.Email))
             .ReturnsAsync((Clinician?)null);

        var result = await service.AddClinician(request);

        Assert.NotNull(result);
        _repo.Verify(r => r.AddClinician(It.IsAny<Clinician>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Test adding a clinician after a user account is created links the existing clinician
    */
    [Fact]
    public async Task AddClinician_ShouldUpdateExistingClinician_WhenClinicianExists()
    {
        var service = CreateService();

        var existing = new Clinician("Old", "Name", "test@test.com");

         var request = new ClinicianRequestDTO("A", "B", "test@test.com", true, 0.5);

        _repo.Setup(r => r.GetClinicianByEmail(request.Email))
             .ReturnsAsync(existing);

        var result = await service.AddClinician(request);

        Assert.NotNull(result);
        _repo.Verify(r => r.AddClinician(It.IsAny<Clinician>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Test getting a clinician by last name returns a list of clinicians with a matching last names
    */
    [Fact]
    public async Task GetClinicianByLastName_ShouldReturnMatchingClinicians()
    {
        var service = CreateService();

        var clinicians = new List<Clinician>
        {
            new Clinician("A", "Smith", "a@test.com"),
            new Clinician("B", "Smith", "b@test.com")
        };

        _repo.Setup(r => r.GetClinicianByLastName("Smith"))
             .ReturnsAsync(clinicians);

        var result = await service.GetClinicianByLastName("Smith");

        Assert.Equal(2, result.Count);
        _repo.Verify(r => r.GetClinicianByLastName("Smith"), Times.Once);
    }

    /*
    Test getting a clinician by last name when no such clinician exists returns an empty list
    */
    [Fact]
    public async Task GetClinicianByLastName_ShouldReturnEmptyList_WhenNoMatches()
    {
        var service = CreateService();

        _repo.Setup(r => r.GetClinicianByLastName("Smith"))
             .ReturnsAsync(new List<Clinician>());

        var result = await service.GetClinicianByLastName("Smith");

        Assert.Empty(result);
    }

    /*
    Test getting all clinicians returns a list of clinicians if they existed
    */
    [Fact]
    public async Task GetClinicians_ShouldReturnClinicians_WhenTheyExist()
    {
        var service = CreateService();

        var clinicians = new List<Clinician>
        {
            new Clinician("A", "B", "a@test.com"),
            new Clinician("C", "D", "c@test.com")
        };

        _repo.Setup(r => r.GetAllClinicians())
             .ReturnsAsync(clinicians);

        var result = await service.GetClinicians();

        Assert.Equal(2, result.Count);
        _repo.Verify(r => r.GetAllClinicians(), Times.Once);
    }

    /*
    Testing getting all clinicians returns an empty list if no clinicians existed
    */
    [Fact]
    public async Task GetClinicians_ShouldReturnEmptyList_WhenNoneExist()
    {
        var service = CreateService();

        _repo.Setup(r => r.GetAllClinicians())
             .ReturnsAsync(new List<Clinician>());

        var result = await service.GetClinicians();

        Assert.Empty(result);
    }

    /*
    Test getting a clinician by ID returns the clinician if the clinician existed
    */
    [Fact]
    public async Task GetClinicianByID_ShouldReturnClinician_WhenExists()
    {
        var service = CreateService();

        var clinician = new Clinician("A", "B", "a@test.com");

        _repo.Setup(r => r.GetClinicianByID(It.IsAny<Guid>()))
             .ReturnsAsync(clinician);

        var result = await service.GetClinicianByID(Guid.NewGuid());

        Assert.NotNull(result);
    }

    /*
    Test getting a clinician by ID returns null if the clinician does not exist
    */
    [Fact]
    public async Task GetClinicianByID_ShouldReturnNull_WhenNotExists()
    {
        var service = CreateService();

        _repo.Setup(r => r.GetClinicianByID(It.IsAny<Guid>()))
             .ReturnsAsync((Clinician?)null!);

        var result = await service.GetClinicianByID(Guid.NewGuid());

        Assert.Null(result);
    }
}