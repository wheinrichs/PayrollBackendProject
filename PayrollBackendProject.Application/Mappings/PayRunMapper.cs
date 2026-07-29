using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class PayRunMapper
    {
        public static (DateTime, DateTime) DTOToDates(PayRunRequestDTO dto)
        {
            return (
                DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc),
                DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc)
            );
        }

        public static DateTime? DTOToPaymentDate(PayRunRequestDTO dto)
        {
            return dto.PaymentDate.HasValue
                ? DateTime.SpecifyKind(dto.PaymentDate.Value.Date, DateTimeKind.Utc)
                : null;
        }

        public static PayRunResponseDTO DomainToDTO(PayRun run)
        {
            return new PayRunResponseDTO(
                run.Id, run.StartDate, run.EndDate, run.PaymentDate,
                run.TotalApplied, run.TotalAdjudicated,
                run.GrossPaymentTotal, run.TotalCode500Deductions,
                run.StatementTotals, run.TotalPsychTodayPayout, run.TotalPayout,
                run.GenerationStatus, run.ApprovalState,
                run.ApprovedRejectedBy, run.ApprovedRejectedOn);
        }
    }
}
