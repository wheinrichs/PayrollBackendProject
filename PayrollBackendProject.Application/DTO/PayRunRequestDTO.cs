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
    }
}