using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class PayStatementMapper
    {
        public static PayStatementDTO DomainToDTO(PayStatement statement, Guid payRunId)
        {
            ClinicianResponseDTO clinicianDTO = ClinicianMapper.DomainToDTO(statement.Clinician);
            var lineItems = statement.LineItems
                .Select(li => new PaymentLineItemDTO
                {
                    Id = li.PaymentLineItemId,
                    PaymentAmount = li.PaymentAmount,
                    AdjustmentAmount = li.AdjustmentAmount,
                    CPTCode = li.CPTCode,
                    PatientId = li.PatientId,
                    DateOfService = li.DateOfService,
                    Payer = li.Payer,
                    AdjustmentCode = (int)li.AdjustmentCode,
                    PaymentId = li.PaymentId
                })
                .ToList();
            return new PayStatementDTO(
                statement.Id,
                clinicianDTO,
                lineItems,
                payRunId,
                statement.TotalPayment,
                statement.CostShareAdjustedPayment,
                statement.TotalAdjustment,
                statement.ClinicianCostShare,
                (int)statement.ApprovalState,
                statement.ApprovedRejectedBy,
                statement.ApprovedRejectedOn);
        }
    }
}
