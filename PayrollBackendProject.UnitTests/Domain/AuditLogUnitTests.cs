using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class AuditLogUnitTests
{
    /*
    Test that a log stores the passed in parameters correctly
    Test that setting the string parameters with empty works correctly
    Test that different Enums are set correctly
    */
    [Theory]
    [InlineData("entity", AuditLogActionEnum.CREATED, "Original data", "updated data", "NewIDFromUser")]
    [InlineData("", AuditLogActionEnum.CREATED, "", "", "")]
    [InlineData("entity", AuditLogActionEnum.UPDATED, "Original data", "updated data", "NewIDFromUser")]
    [InlineData("entity", AuditLogActionEnum.REJECTED, "Original data", "updated data", "NewIDFromUser")]
    [InlineData("entity", AuditLogActionEnum.APPROVED, "Original data", "updated data", "NewIDFromUser")]

    public void Constructor_ShouldSetProvidedValues(
        string entityName,
        AuditLogActionEnum action,
        string originalData,
        string updatedData,
        string actorId
    )
    {
        Guid entityGuid = Guid.NewGuid();

        var log = new AuditLog(entityName, entityGuid, action, originalData, updatedData, actorId);

        Assert.Equal(entityName, log.EntityName);
        Assert.Equal(entityGuid, log.EntityId);
        Assert.Equal(action, log.Action);
        Assert.Equal(originalData, log.OriginalData);
        Assert.Equal(updatedData, log.UpdatedData);
        Assert.Equal(actorId, log.ActorId);
    }

    /*
    Test that a new Guid is actually assigned
    */
    [Fact]
    public void Constructor_ValidGuid()
    {
        string entityName = "entity";
        Guid entityGuid = Guid.NewGuid();
        AuditLogActionEnum action = AuditLogActionEnum.CREATED;
        string originalData = "Original data";
        string updatedData = "Updated data";
        string actorId = "NewIDFromUser";

        var log = new AuditLog(entityName, entityGuid, action, originalData, updatedData, actorId);

        Assert.NotEqual(Guid.Empty, log.Id);
    }

    /*
    Test that the timestamp is set correctly and is within a reasonable range
    */
    [Fact]
    public void Constructor_SetTimestramp()
    {
        string entityName = "entity";
        Guid entityGuid = Guid.NewGuid();
        AuditLogActionEnum action = AuditLogActionEnum.CREATED;
        string originalData = "Original data";
        string updatedData = "Updated data";
        string actorId = "NewIDFromUser";
        
        DateTime before = DateTime.UtcNow;

        var log = new AuditLog(entityName, entityGuid, action, originalData, updatedData, actorId);

        DateTime after = DateTime.UtcNow;

        Assert.InRange(log.TimestampUTC, before, after);
    }
}
