using Xunit;
using Moq;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.UnitTests;

public class ImportJobUnitTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<ICsvParserService> _parser = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();

    private ImportJob CreateJob()
    {
        return new ImportJob(
            _unitOfWork.Object,
            _paymentRepo.Object,
            _parser.Object,
            _auditRepo.Object
        );
    }

    /*
    Test that when the batch does not exist an exception is thrown
    */
    [Fact]
    public async Task ProcessBatch_BatchNotFound_ThrowsException()
    {
        // Arrange
        var job = CreateJob();
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync((ImportBatch?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => job.ProcessBatch(batchId));
    }

    /*
    Test that processing moves the batch out of initial state
    */
    [Fact]
    public async Task ProcessBatch_ValidFlow_UpdatesStatus()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ReturnsAsync(new UploadResult("filename", 1, 1, 0, new List<string>()));

        // Act
        await job.ProcessBatch(batchId);

        // Assert
        Assert.NotEqual(ImportStatusEnum.PENDING, batch.ImportStatus);
    }

    /*
    Test that an audit log is created when processing begins
    */
    [Fact]
    public async Task ProcessBatch_CreatesInitialAuditLog()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ReturnsAsync(new UploadResult("filename", 1, 1, 0, new List<string>()));

        // Act
        await job.ProcessBatch(batchId);

        // Assert
        _auditRepo.Verify(x => x.AddAuditLog(It.IsAny<AuditLog>()), Times.AtLeast(2));
    }

    /*
    Test that successful parsing sets the batch results
    */
    [Fact]
    public async Task ProcessBatch_ValidParse_SetsResults()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        var result = new UploadResult("filename", 10, 9, 2, new List<string>());

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ReturnsAsync(result);

        // Act
        await job.ProcessBatch(batchId);

        // Assert
        Assert.Equal(10, batch.TotalRows);
        Assert.Equal(9, batch.FailedItems);
    }

    /*
    Test that a null parse result sets the batch status to FAILED
    */
    [Fact]
    public async Task ProcessBatch_NullParse_SetsFailedStatus()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ReturnsAsync((UploadResult?)null);

        // Act
        await job.ProcessBatch(batchId);

        // Assert
        Assert.Equal(ImportStatusEnum.FAILED, batch.ImportStatus);
    }

    /*
    Test that an exception during parsing sets the batch status to FAILED and rethrows
    */
    [Fact]
    public async Task ProcessBatch_ParseThrows_SetsFailedAndRethrows()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ThrowsAsync(new Exception("Parsing failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => job.ProcessBatch(batchId));

        Assert.Equal(ImportStatusEnum.FAILED, batch.ImportStatus);
        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }

    /*
    Test that changes are persisted via UnitOfWork
    */
    [Fact]
    public async Task ProcessBatch_CallsSaveChanges()
    {
        // Arrange
        var job = CreateJob();
        var batch = new ImportBatch("filename", "fingerprint");
        var batchId = Guid.NewGuid();

        _paymentRepo.Setup(x => x.GetImportBatchById(batchId))
            .ReturnsAsync(batch);

        _parser.Setup(x => x.Parse(batch))
            .ReturnsAsync(new UploadResult("filename", 1, 1, 0, new List<string>()));

        // Act
        await job.ProcessBatch(batchId);

        // Assert
        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.AtLeast(2));
    }
}