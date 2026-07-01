namespace PayrollBackendProject.Application.DTO
{
    public class ManualPaymentRequestDTO
    {
        public decimal? PaymentAmount { get; set; }
        public decimal? AdjustmentAmount { get; set; }
        public int PaymentAdjustmentCode { get; set; }
        public DateTime DateOfService { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string CPTCode { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public string? Payer { get; set; }
        public string RawClinicianName { get; set; } = string.Empty;
        public Guid? ClinicianId { get; set; }
        public DateTime AppliedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
