using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class PayRunMapper
    {
        public static (DateTime, DateTime) DTOToDates(PayRunRequestDTO dto)
        {
            return (dto.StartDate.Date, dto.EndDate.Date);
        } 

        public static PayRunResponseDTO DomainToDTO(PayRun run)
        {
            return new PayRunResponseDTO(run.Id, run.StartDate, run.EndDate, run.TotalApplied, run.TotalAdjudicated, run.StatementTotals, run.GenerationStatus, run.ApprovalState, run.ApprovedRejectedBy, run.ApprovedRejectedOn);
        }
    }
}
