using PayrollBackendProject.Application.Interfaces.Services;
using Hangfire;

namespace PayrollBackendProject.Infrastructure.BackgroundJobs
{
    public class HangfireBackgroundJobService : IBackgroundJobService
    {
        private readonly IBackgroundJobClient _client;
        public HangfireBackgroundJobService(IBackgroundJobClient client)
        {
            _client = client;
        }
        public void EnqueueImportBatchParsingJob(Guid importBatchId)
        {
            _client.Enqueue<IImportJob>(job => job.ProcessBatch(importBatchId));
        }
    }
}
