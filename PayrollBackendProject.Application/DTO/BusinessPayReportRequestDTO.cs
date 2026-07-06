namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the data required to generate a business pay report over a date range.
    /// </summary>
    /// <remarks>
    /// Unlike a pay run, generating a business pay report does not create or persist any
    /// pay run or pay statement records - it only computes totals for review.
    /// </remarks>
    public class BusinessPayReportRequestDTO
    {
        /// <summary>
        /// Gets or sets the start date of the report period.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the report period.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets whether a flat Psych Today payout should be previewed for every eligible clinician.
        /// </summary>
        public bool IncludePsychTodayPayout { get; set; } = false;

        /// <summary>
        /// Gets or sets the flat per-clinician Psych Today payout amount.
        /// </summary>
        /// <remarks>
        /// Required and must be greater than 0 when <see cref="IncludePsychTodayPayout"/> is true; otherwise ignored.
        /// </remarks>
        public decimal? PsychTodayPayoutAmount { get; set; }
    }
}
