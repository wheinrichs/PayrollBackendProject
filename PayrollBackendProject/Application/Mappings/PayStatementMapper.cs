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
                    PatientId = li.PatientId
                })
                .ToList();
            return new PayStatementDTO(statement.Id, clinicianDTO, lineItems, payRunId, statement.TotalPayment, statement.CostShareAdjustedPayment, statement.TotalAdjustment);
        }
    }
}
