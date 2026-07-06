namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the read-only totals produced by a business pay report over a date range.
    /// </summary>
    /// <remarks>
    /// Unlike a pay run, generating this report never creates or persists a pay run or pay statement.
    /// The code-500 (insurance takeback) totals reflect deductions already applied via real, completed
    /// pay runs whose applied date falls in this range - the report does not simulate hypothetical new
    /// code-500 applications, since applying a takeback is a deliberate, itemized action tied to a real pay run.
    /// </remarks>
    public class BusinessPayReportResponseDTO
    {
        /// <summary>
        /// Gets the start date of the report period.
        /// </summary>
        public DateTime StartDate { get; private set; }

        /// <summary>
        /// Gets the end date of the report period.
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// Gets the total applied payment amount before adjudication.
        /// </summary>
        public decimal TotalApplied { get; private set; }

        /// <summary>
        /// Gets the total adjudicated payment amount after processing adjustments.
        /// </summary>
        public decimal TotalAdjudicated { get; private set; }

        /// <summary>
        /// Gets the total gross payment across all clinicians before cost share deductions.
        /// </summary>
        public decimal GrossPaymentTotal { get; private set; }

        /// <summary>
        /// Gets the total value of code-500 (insurance takeback) deductions already applied via real pay runs
        /// whose applied date falls within this report's range.
        /// </summary>
        public decimal TotalCode500Deductions { get; private set; }

        /// <summary>
        /// Gets the count of code-500 (insurance takeback) applications already applied within this report's range.
        /// </summary>
        public int TotalCode500LineItemCount { get; private set; }

        /// <summary>
        /// Gets the net payout to all clinicians after cost share adjustments, before any Psych Today payout.
        /// </summary>
        public decimal StatementTotals { get; private set; }

        /// <summary>
        /// Gets the total flat Psych Today payout previewed across all eligible clinicians.
        /// </summary>
        public decimal TotalPsychTodayPayout { get; private set; }

        /// <summary>
        /// Gets the combined total amount that would be paid out to all clinicians for this range,
        /// including cost share adjustments and the Psych Today payout.
        /// </summary>
        public decimal TotalPayout { get; private set; }

        /// <summary>
        /// Gets the per-clinician breakdown for this report.
        /// </summary>
        public List<ClinicianPayReportEntryDTO> ClinicianBreakdown { get; private set; }

        public BusinessPayReportResponseDTO(
            DateTime startDate,
            DateTime endDate,
            decimal totalApplied,
            decimal totalAdjudicated,
            decimal grossPaymentTotal,
            decimal totalCode500Deductions,
            int totalCode500LineItemCount,
            decimal statementTotals,
            decimal totalPsychTodayPayout,
            decimal totalPayout,
            List<ClinicianPayReportEntryDTO> clinicianBreakdown)
        {
            StartDate = startDate;
            EndDate = endDate;
            TotalApplied = totalApplied;
            TotalAdjudicated = totalAdjudicated;
            GrossPaymentTotal = grossPaymentTotal;
            TotalCode500Deductions = totalCode500Deductions;
            TotalCode500LineItemCount = totalCode500LineItemCount;
            StatementTotals = statementTotals;
            TotalPsychTodayPayout = totalPsychTodayPayout;
            TotalPayout = totalPayout;
            ClinicianBreakdown = clinicianBreakdown;
        }
    }
}
