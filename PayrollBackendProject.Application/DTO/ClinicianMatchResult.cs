namespace PayrollBackendProject.Application.DTO
{
    public class ClinicianMatchResult
    {
        public int TotalUnmatchedEntries { get; set; }
        public int FailedRows { get; set; }
        public int ResolvedRows { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        
        public ClinicianMatchResult(int totalUnmatchedEntries)
        {
            TotalUnmatchedEntries = totalUnmatchedEntries;
            FailedRows = 0;
            ResolvedRows = 0;
        }
    }
}
