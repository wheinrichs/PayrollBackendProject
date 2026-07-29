namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// The generated CSV export of approved pay statements along with its download metadata.
    /// </summary>
    public class StatementExportResultDTO
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string FileName { get; init; } = string.Empty;
        public int StatementCount { get; init; }
        public int RowCount { get; init; }
    }
}
