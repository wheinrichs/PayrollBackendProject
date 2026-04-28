namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the result of attempting to match clinicians to unresolved payment line items.
    /// </summary>
    /// <remarks>
    /// This DTO summarizes the outcome of the matching process, including counts of resolved and failed entries,
    /// along with any errors encountered during processing.
    /// </remarks>
    public class ClinicianMatchResult
    {
        /// <summary>
        /// Gets or sets the total number of payment line items that initially had no matched clinician.
        /// </summary>
        public int TotalUnmatchedEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of rows that failed to resolve during the matching process.
        /// </summary>
        public int FailedRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows successfully matched to a clinician.
        /// </summary>
        public int ResolvedRows { get; set; }

        /// <summary>
        /// Gets or sets the list of errors encountered while attempting to resolve clinician matches.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicianMatchResult"/> class.
        /// </summary>
        /// <param name="totalUnmatchedEntries">
        /// The total number of payment line items that initially had no matched clinician.
        /// </param>
        public ClinicianMatchResult(int totalUnmatchedEntries)
        {
            TotalUnmatchedEntries = totalUnmatchedEntries;
            FailedRows = 0;
            ResolvedRows = 0;
        }
    }
}