namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// One flattened CSV row of the approved-statements export: a single statement line item
    /// with its statement-level and pay-run-level context repeated on every row.
    /// </summary>
    /// <remarks>
    /// This schema is the contract consumed by the local statement rebuild tool — column names
    /// and formats must stay in sync with it.
    /// Amounts use the stored domain sign convention (raw report values are negated at import):
    /// positive PaymentAmount = clinician earnings; 500-code snapshots carry PaymentAmount = 0
    /// and a negative AdjustmentAmount.
    /// </remarks>
    public class StatementExportRowDTO
    {
        public Guid PayRunId { get; set; }
        public DateTime PayRunStartDate { get; set; }
        public DateTime PayRunEndDate { get; set; }
        public DateTime PayRunPaymentDate { get; set; }
        public Guid PayStatementId { get; set; }
        public Guid ClinicianId { get; set; }
        public string ClinicianFirstName { get; set; } = string.Empty;
        public string ClinicianLastName { get; set; } = string.Empty;
        public string ClinicianEmail { get; set; } = string.Empty;
        public decimal CostShareSnapshot { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal TotalAdjustment { get; set; }
        public decimal Code500Deductions { get; set; }
        public decimal CostShareAdjustedPayment { get; set; }
        public decimal PsychTodayPayout { get; set; }
        public decimal TotalPayout { get; set; }
        public DateTime? StatementApprovedOn { get; set; }
        public Guid LineItemId { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public DateTime DateOfService { get; set; }
        public string CPTCode { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Payer { get; set; } = string.Empty;
        public int AdjustmentCode { get; set; }
        public string AdjustmentCodeName { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public DateTime AppliedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public int RowNumber { get; set; }
    }
}
