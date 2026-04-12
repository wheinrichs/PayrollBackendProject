using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.DTO
{
    public class PayStatementDTO
    {
        public Guid Id { get; private set; }
        public ClinicianResponseDTO Clinician { get; private set; }
        public List<PaymentLineItemDTO> LineItems { get; private set; }
        public Guid PayRun { get; private set; }

        public decimal TotalPayment { get; private set; }
        public decimal CostShareAdjustedPayment { get; private set; }
        public decimal TotalAdjustment { get; private set; }

        public PayStatementDTO(Guid id, ClinicianResponseDTO clinician, List<PaymentLineItemDTO> lineItems, Guid payRun, decimal totalPayment, decimal costShareAdjustedPayment, decimal totalAdjustment)
        {
            Id = id;
            Clinician = clinician;
            LineItems = lineItems;
            PayRun = payRun;
            TotalPayment = totalPayment;
            CostShareAdjustedPayment = costShareAdjustedPayment;
            TotalAdjustment = totalAdjustment;
        }
    }
}
