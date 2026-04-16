using Moq;
using Xunit;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.UnitTests;

public class ImportServiceUnitTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();
    private readonly Mock<IUserAccountRepository> _userRepo = new();
    private readonly Mock<IEHRUserAccountRepository> _ehrRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IFingerprintGenerator> _fingerprint = new();
    private readonly Mock<IFileHandler> _fileHandler = new();

    private ImportService CreateService()
    {
        return new ImportService(
            _repo.Object,
            _clinicianRepo.Object,
            _userRepo.Object,
            _ehrRepo.Object,
            _unitOfWork.Object,
            _auditRepo.Object,
            _fingerprint.Object,
            _fileHandler.Object
        );
    }

    /*
    Test that adding the same file twice returns the batch id of the first file
    */
    [Fact]
    public async Task CreateBatchAndStoreFile_ShouldReturnExistingBatchId_WhenFingerprintExists()
    {
        var service = CreateService();

        var stream = new MemoryStream();
        var fingerprint = "abc123";
        var existingBatch = new ImportBatch("file.csv", fingerprint);

        _fingerprint.Setup(f => f.FileComputeSHA256Async(It.IsAny<Stream>()))
            .ReturnsAsync(fingerprint);

        _repo.Setup(r => r.GetImportBatch(fingerprint))
            .ReturnsAsync(existingBatch);

        var result = await service.CreateBatchAndStoreFile(stream, "file.csv", "root", Guid.NewGuid());

        Assert.Equal(existingBatch.Id, result);
        _fileHandler.Verify(f => f.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _repo.Verify(r => r.AddBatchItem(It.IsAny<ImportBatch>()), Times.Never);
    }

    /*
    Test that when adding a new batch the file write is attempted
    */
    [Fact]
    public async Task CreateBatchAndStoreFile_ShouldWriteFile_WhenNewBatch()
    {
        var service = CreateService();

        var stream = new MemoryStream();
        var fingerprint = "abc123";

        _fingerprint.Setup(f => f.FileComputeSHA256Async(It.IsAny<Stream>()))
            .ReturnsAsync(fingerprint);

        _repo.Setup(r => r.GetImportBatch(fingerprint))
            .ReturnsAsync((ImportBatch?)null);

        _fileHandler.Setup(f => f.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync("path/file.csv");

        var result = await service.CreateBatchAndStoreFile(stream, "file.csv", "root", Guid.NewGuid());

        _fileHandler.Verify(f => f.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
        _repo.Verify(r => r.AddBatchItem(It.IsAny<ImportBatch>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /*
    Test that a new log is created and written in the expected format when creating a new batch
    */
    [Fact]
    public async Task CreateBatchAndStoreFile_ShouldCreateAuditLog_WhenNewBatch()
    {
        var service = CreateService();

        var stream = new MemoryStream();
        var fingerprint = "abc123";

        _fingerprint.Setup(f => f.FileComputeSHA256Async(It.IsAny<Stream>()))
            .ReturnsAsync(fingerprint);

        _repo.Setup(r => r.GetImportBatch(fingerprint))
            .ReturnsAsync((ImportBatch?)null);

        _fileHandler.Setup(f => f.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync("path/file.csv");

        var userId = Guid.NewGuid();

        var result = await service.CreateBatchAndStoreFile(stream, "file.csv", "root", userId);

        _auditRepo.Verify(a => a.AddAuditLog(It.Is<AuditLog>(log =>
            log.EntityName == "Import Batch" &&
            log.Action == AuditLogActionEnum.CREATED &&
            log.ActorId == userId.ToString()
        )), Times.Once);
    }

    /*
    Test when retrieving a batch status if it does not exist it returns null
    */
    [Fact]
    public async Task GetBatchStatus_ShouldReturnNull_WhenBatchDoesNotExist()
    {
        var service = CreateService();

        _repo.Setup(r => r.GetImportBatchById(It.IsAny<Guid>()))
            .ReturnsAsync((ImportBatch?)null);

        var result = await service.GetBatchStatus(Guid.NewGuid());

        Assert.Null(result);
    }

    /*
    Test when retrieving a batch status if it does exist it returns the batch status
    */
    [Fact]
    public async Task GetBatchStatus_ShouldReturnUploadResult_WhenBatchExists()
    {
        var service = CreateService();

        var batch = new ImportBatch("file.csv", "fingerprint");
        var errors = new List<string> { "error1" };
        batch.SetResults(5, 70, 200, 3, errors);


        _repo.Setup(r => r.GetImportBatchById(It.IsAny<Guid>()))
            .ReturnsAsync(batch);

        var result = await service.GetBatchStatus(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(batch.Filename, result!.Filename);
        Assert.Equal(batch.TotalRows, result.TotalRows);
        Assert.Equal(batch.FailedItems, result.FailedRows);
        Assert.Equal(batch.UnresolvedRows, result.UnresolvedRows);
    }
}