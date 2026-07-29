using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IStatementExportService
    {
        public Task<StatementExportResultDTO> ExportApprovedStatementsCsv(Guid payRunGuid, Guid userId);
    }
}
