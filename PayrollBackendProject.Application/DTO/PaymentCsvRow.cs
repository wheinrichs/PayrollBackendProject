namespace PayrollBackendProject.Application.DTO
{
    public class PaymentCsvRow
    {
        public DateTime? AppliedDate { get; set; }
        public string PatientID { get; set; } = string.Empty;
        public string? PatientName { get; set; }
        public DateTime DOS { get; set; }
        public string CPT { get; set; } = string.Empty;
        public string? PaymentID { get; set; }
        public decimal? AppliedPayments { get; set; }
        public decimal? AppliedAdjustment { get; set; }
        public string? Desc { get; set; }
        public string AcctNo { get; set; } = string.Empty;
        public string? Payer { get; set; }
        public string? AppliedBy { get; set; }
        public string ClinicianName { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
    }
}
