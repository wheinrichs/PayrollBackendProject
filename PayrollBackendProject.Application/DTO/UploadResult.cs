namespace PayrollBackendProject.Application.DTO
{
    public class UploadResult
    {
        public string Filename { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int FailedRows { get; set; }
        public int SkippedRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int UnresolvedRows { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public UploadResult(string filename)
        {
            Filename = filename;
        }
        
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
