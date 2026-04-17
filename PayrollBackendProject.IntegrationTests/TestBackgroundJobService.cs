using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.IntegrationTests;

public class TestBackgroundJobService : IBackgroundJobService
{
    public void EnqueueImportBatchParsingJob(Guid batchId)
    {
        // Do nothing in tests
    }
}