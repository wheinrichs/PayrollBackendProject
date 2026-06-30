using System;
using System.Collections.Generic;
using System.Text;

namespace PayrollBackendProject.Application.Interfaces.Utilities
{
    public interface IFingerprintGenerator
    {
        public Task<string> FileComputeSHA256Async(Stream stream);
        public Task<string> LineItemComputeSHA256Async(string rawData, string batchId, string rowNumber);
        public Task<string> ManualEntryComputeSHA256Async(string patientId, string cptCode, string paymentId, DateTime dateOfService, int adjustmentCode);
    }
}
