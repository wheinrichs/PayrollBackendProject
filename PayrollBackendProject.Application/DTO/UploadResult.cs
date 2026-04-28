namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the result of processing an uploaded CSV file.
    /// </summary>
    /// <remarks>
    /// This DTO summarizes the outcome of a file upload and parsing operation,
    /// including row-level processing results and any errors encountered.
    /// </remarks>
    public class UploadResult
    {
        /// <summary>
        /// Gets or sets the name of the uploaded file.
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of rows processed from the file.
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows that failed during processing.
        /// </summary>
        public int FailedRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows that were skipped during processing.
        /// </summary>
        public int SkippedRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows successfully processed.
        /// </summary>
        public int SuccessfulRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows that could not be resolved (e.g., unmatched clinician).
        /// </summary>
        public int UnresolvedRows { get; set; }

        /// <summary>
        /// Gets or sets the list of errors encountered during processing.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadResult"/> class with a filename.
        /// </summary>
        /// <param name="filename">The name of the uploaded file.</param>
        public UploadResult(string filename)
        {
            Filename = filename;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UploadResult"/> class with summary details.
        /// </summary>
        /// <param name="filename">The name of the uploaded file.</param>
        /// <param name="totalRows">The total number of rows processed.</param>
        /// <param name="failedRows">The number of rows that failed processing.</param>
        /// <param name="unresovledRows">The number of rows that could not be resolved.</param>
        /// <param name="errors">A list of errors encountered during processing.</param>
        public UploadResult(string filename, int totalRows, int failedRows, int unresovledRows, List<string> errors)
        {
            Filename = filename;
            TotalRows = totalRows;
            FailedRows = failedRows;
            UnresolvedRows = unresovledRows;
            Errors = errors;
        }
    }
}