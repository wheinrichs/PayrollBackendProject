using PayrollBackendProject.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace PayrollBackendProject.Domain.Entity
{
    public class ImportBatch
    {
        public Guid Id { get; private set; }

        public string Filename { get; private set; } = string.Empty;

        public DateTime UploadTime { get; private set; }

        public DateTime? StatusTime { get; private set; }

        public ImportStatusEnum ImportStatus { get; private set; }

        public int FailedItems { get; private set; }
        public int SuccessfulItems { get; private set; }
        public int TotalRows { get; private set; }
        public int UnresolvedRows { get; private set; }
        // TODO transition this from a list of strings to a list of a new entity called errors
        public List<string> Errors { get; private set; } = new List<string>();


        public string Fingerprint { get; private set; } = string.Empty;

        public string Filepath { get; private set; } = string.Empty;

        private ImportBatch() { }

        public ImportBatch(string filename, string fingerprint)
        {
            if(string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(fingerprint))
            {
                throw new ArgumentException("The filename and fingerprint cannot be empty.");
            }
            Id = Guid.NewGuid();
            Filename = filename;
            UploadTime = DateTime.UtcNow;
            ImportStatus = ImportStatusEnum.PENDING;
            FailedItems = 0;
            TotalRows = 0;
            SuccessfulItems = 0;
            Fingerprint = fingerprint;
        }

        public void UpdateStatus(ImportStatusEnum status)
        {
            StatusTime = DateTime.UtcNow;
            ImportStatus = status;
        }

        public void SetResults(int failedRows, int successfulRows, int totalRows, int unresolvedRows, List<String> errors)
        {
            if(failedRows < 0 || failedRows > totalRows)
            {
                throw new ArgumentException("Failed rows cannot be negative or larger than total rows.");
            }
            if(successfulRows < 0 || successfulRows > totalRows)
            {
                throw new ArgumentException("Successful rows cannot be negative or larger than total rows.");
            }
            if(unresolvedRows < 0 || unresolvedRows > totalRows)
            {
                throw new ArgumentException("Unresolved rows cannot be negative or larger than total rows.");
            }
            
            FailedItems = failedRows;
            SuccessfulItems = successfulRows;
            TotalRows = totalRows;
            UnresolvedRows = unresolvedRows;
            Errors = errors;

            if (SuccessfulItems == totalRows && UnresolvedRows == 0)
            {
                UpdateStatus(ImportStatusEnum.SUCCESS);
            }
            else if ((SuccessfulItems != 0 && FailedItems != 0) || UnresolvedRows != 0)
            {
                UpdateStatus(ImportStatusEnum.SUCCESS_WITH_ERRORS);
            }
            else
            {
                UpdateStatus(ImportStatusEnum.FAILED);
            }
        }

        public void AssignFilepath(string filepath)
        {
            if(string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("Filepath cannot be null or empty.");
            }
            Filepath = filepath;
        }
    }
}