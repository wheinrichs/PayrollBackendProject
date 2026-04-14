namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IImportJob
    {
        public Task ProcessBatch(Guid batchId);
    }
}
