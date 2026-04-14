namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IBackgroundJobService
    {
        public void EnqueueImportBatchParsingJob(Guid importBatchId);
    }
}
