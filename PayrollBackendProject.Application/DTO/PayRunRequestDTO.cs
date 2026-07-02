using System.ComponentModel.DataAnnotations;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the data required to create a new pay run.
    /// </summary>
    /// <remarks>
    /// A pay run defines a date range over which payment line items are aggregated
    /// and processed into pay statements for clinicians.
    /// </remarks>
    public class PayRunRequestDTO
    {
        /// <summary>
        /// Gets or sets the start date of the pay run period.
        /// </summary>
        /// <remarks>
        /// This represents the beginning of the time range for which payments will be included.
        /// </remarks>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the pay run period.
        /// </summary>
        /// <remarks>
        /// This represents the end of the time range for which payments will be included.
        /// It is expected to be later than <see cref="StartDate"/>.
        /// </remarks>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets whether a flat Psych Today payout should be added to every eligible clinician's pay statement.
        /// </summary>
        /// <remarks>
        /// Eligible clinicians are those with <c>HasPsychToday</c> set to true who already have a pay statement in this run.
        /// </remarks>
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