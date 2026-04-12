namespace PayrollBackendProject.Application.DTO
{
    public class PaymentCsvRow
    {
        public DateTime? AppliedDate { get; set; }
        public string PatientID { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime DOS { get; set; }
        public string CPT { get; set; } = string.Empty;
        public string? PaymentID { get; set; } = string.Empty;
        public decimal? AppliedPayments {  get; set; }
        public decimal? AppliedAdjustment { get; set; }
        public string Desc { get; set; } = string.Empty;
        public string AcctNo { get; set; } = string.Empty;
        public string Payer { get; set; } = string.Empty;
        public string AppliedBy { get; set; } = string.Empty;
        public string ClinicianName { get; set; } = string.Empty;
        public DateTime? PaymentDate {  get; set; }


    }
}
