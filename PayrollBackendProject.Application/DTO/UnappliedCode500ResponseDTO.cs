namespace PayrollBackendProject.Application.DTO
{
    public class UnappliedCode500ResponseDTO
    {
        public Guid Id { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public DateTime DateOfService { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string CPTCode { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Payer { get; set; } = string.Empty;
        public string RawClinicianName { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public Guid? ImportBatchId { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
