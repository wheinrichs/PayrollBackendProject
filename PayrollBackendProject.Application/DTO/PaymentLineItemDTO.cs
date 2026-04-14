namespace PayrollBackendProject.Application.DTO
{
    public class PaymentLineItemDTO
    {
        public Guid Id { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public string? CPTCode { get; set; }
        public string? PatientId { get; set; }
    }
}
