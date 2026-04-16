namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IFileHandler
    {
        public Task<string> WriteFile(Stream fileStream, string filename, string uploadRoot, Guid batchId);
    }
}
