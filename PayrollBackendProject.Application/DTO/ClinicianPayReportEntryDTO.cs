namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents one clinician's totals within a business pay report.
    /// </summary>
    /// <remarks>
    /// Mirrors the totals computed for a real pay statement, but is never persisted -
    /// it exists only to preview what a pay run over the same date range would produce.
    /// </remarks>
    public class ClinicianPayReportEntryDTO
    {
        /// <summary>
        /// Gets the unique identifier of the clinician.
        /// </summary>
        public Guid ClinicianId { get; private set; }

        /// <summary>
        /// Gets the clinician's first name.
        /// </summary>
        public string FirstName { get; private set; }

        /// <summary>
        /// Gets the clinician's last name.
        /// </summary>
        public string LastName { get; private set; }

        /// <summary>
        /// Gets the total payment amount before adjustments.
        /// </summary>
        public decimal TotalPayment { get; private set; }

        /// <summary>
        /// Gets the total adjustment amount applied to the payment.
        /// </summary>
        public decimal TotalAdjustment { get; private set; }

        /// <summary>
        /// Gets the total value of applied code-500 (insurance takeback) deductions for this clinician.
        /// </summary>
        public decimal Code500Deductions { get; private set; }

        /// <summary>
        /// Gets the count of applied code-500 (insurance takeback) line items for this clinician.
        /// </summary>
        public int Code500LineItemCount { get; private set; }

        /// <summary>
        /// Gets the total payment amount after applying cost share adjustments.
        /// </summary>
        public decimal CostShareAdjustedPayment { get; private set; }

        /// <summary>
        /// Gets the flat Psych Today payout previewed for this clinician, if any.
        /// </summary>
        public decimal PsychTodayPayout { get; private set; }

        /// <summary>
        /// Gets the combined total payout for this clinician, including cost share adjustments and the Psych Today payout.
        /// </summary>
        public decimal TotalPayout { get; private set; }

        /// <summary>
        /// Gets the clinician's cost share percentage at the time the report was generated.
        /// </summary>
        public decimal CostShareSnapshot { get; private set; }

        public ClinicianPayReportEntryDTO(
            Guid clinicianId,
            string firstName,
            string lastName,
            decimal totalPayment,
            decimal totalAdjustment,
            decimal code500Deductions,
            int code500LineItemCount,
            decimal costShareAdjustedPayment,
            decimal psychTodayPayout,
            decimal totalPayout,
            decimal costShareSnapshot)
        {
            ClinicianId = clinicianId;
            FirstName = firstName;
            LastName = lastName;
            TotalPayment = totalPayment;
            TotalAdjustment = totalAdjustment;
            Code500Deductions = code500Deductions;
            Code500LineItemCount = code500LineItemCount;
            CostShareAdjustedPayment = costShareAdjustedPayment;
            PsychTodayPayout = psychTodayPayout;
            TotalPayout = totalPayout;
            CostShareSnapshot = costShareSnapshot;
        }
    }
}
