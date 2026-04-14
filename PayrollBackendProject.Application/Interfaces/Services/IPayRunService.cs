using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IPayRunService
    {
        public Task<PayRunResponseDTO> ExecutePayRun(PayRunRequestDTO request, Guid userId);
        public Task<List<PayStatementDTO>> RetrievePayStatementsForRun(Guid payRunGuid);
        public Task ApprovePayRun(Guid payRunGuid, Guid approverGuid);
        public Task ApprovePayStatement(Guid payStatementGuid, Guid approverGuid);
        public Task<List<PayStatementDTO>> RetrieveStatementsForUser(Guid userId);
    }
}
