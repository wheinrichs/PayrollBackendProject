using Moq;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class AuditLogServiceUnitTests
{
    /*
    Validate that retrieving the logs hits the repository the correct amount of times and that
    if logs are present in the repository they are returned
    */
    [Fact]
    public async Task GetAllLogs_ShouldReturnCorrectValues()
    {
        Mock<IAuditLogRepository> mockRepo = new();
        List<AuditLog> domainLogs = new();
        domainLogs.Add(new AuditLog("PayRun", Guid.NewGuid(), AuditLogActionEnum.CREATED, "Original Data", "New Data", "ActorId"));
        domainLogs.Add(new AuditLog("PayStatement", Guid.NewGuid(), AuditLogActionEnum.UPDATED, "Statement 1", "New Statement", "ActorId2"));

        mockRepo.Setup(r => r.GetAllAuditLogs()).ReturnsAsync(domainLogs);

        AuditLogService auditLogService = new(mockRepo.Object);

        var returnedItems = await auditLogService.GetAllLogs();

        List<AuditLogDTO> expectedReturns = domainLogs.Select(i => AuditLogMapper.DomainToDto(i)).ToList();

        Assert.Equal(expectedReturns.Count, returnedItems.Count);
        Assert.Equal(expectedReturns[0].EntityName, returnedItems[0].EntityName);
        mockRepo.Verify(r => r.GetAllAuditLogs(), Times.Once);
    }

    /*
    Validate that if there are no logs in the database that nothing is returned
    */
    [Fact]
    public async Task GetAllLogs_ShouldReturnNoValues()
    {
        Mock<IAuditLogRepository> mockRepo = new();
        List<AuditLog> domainLogs = new();
        
        mockRepo.Setup(r => r.GetAllAuditLogs()).ReturnsAsync(domainLogs);

        AuditLogService auditLogService = new(mockRepo.Object);

        var returnedItems = await auditLogService.GetAllLogs();

        Assert.Empty(returnedItems);
        mockRepo.Verify(r => r.GetAllAuditLogs(), Times.Once);
    }

    /*
    Validate that getting audit logs by an ID only returns logs with that ID
    */
    [Fact]
    public async Task GetLogsByEntityId_ShouldReturnLogsFromRepository_WhenLogsExist()
    {
        Mock<IAuditLogRepository> mockRepo = new();
        Guid selectedGuid = Guid.NewGuid();
        AuditLog selectedLog = new AuditLog("PayRun", selectedGuid, AuditLogActionEnum.CREATED, "Original Data", "New Data", "ActorId");

        mockRepo.Setup(r => r.GetAuditLogsByEntityId(selectedGuid)).ReturnsAsync(new List<AuditLog> {selectedLog});
        
        AuditLogService auditLogService = new(mockRepo.Object);

        var returnedItems = await auditLogService.GetLogsByEntityId(selectedGuid);
        var expectedReturns = new List<AuditLogDTO> {AuditLogMapper.DomainToDto(selectedLog)};
        Assert.Equal(expectedReturns.Count, returnedItems.Count);
        Assert.Equal(expectedReturns[0].EntityName, returnedItems[0].EntityName);
        mockRepo.Verify(r => r.GetAuditLogsByEntityId(selectedGuid), Times.Once);
    }

    /*
    Validate that nothing is returned if there are no audit logs with a matching ID
    */
        [Fact]
    public async Task GetLogsByEntityId_ShouldReturnNothing_WhenLogsDoNotExist()
    {
        Mock<IAuditLogRepository> mockRepo = new();
        Guid selectedGuid = Guid.NewGuid();

        mockRepo.Setup(r => r.GetAuditLogsByEntityId(selectedGuid)).ReturnsAsync(new List<AuditLog> {});
        
        AuditLogService auditLogService = new(mockRepo.Object);

        var returnedItems = await auditLogService.GetLogsByEntityId(selectedGuid);
        var expectedReturns = new List<AuditLogDTO> {};
        Assert.Equal(expectedReturns.Count, returnedItems.Count);
        mockRepo.Verify(r => r.GetAuditLogsByEntityId(selectedGuid), Times.Once);
    }
}