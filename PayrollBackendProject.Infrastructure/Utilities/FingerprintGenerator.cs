using System.Security.Cryptography;
using System.Text;
using PayrollBackendProject.Application.Interfaces.Utilities;

namespace PayrollBackendProject.Infrastructure.Utilities
{
    public class FingerprintGenerator : IFingerprintGenerator
    {
        public async Task<string> FileComputeSHA256Async(Stream stream)
        {
            using var sha256 = SHA256.Create();

            byte[] hash = await sha256.ComputeHashAsync(stream);

            return Convert.ToHexString(hash);
        }

        public async Task<string> LineItemComputeSHA256Async(string rawData, string batchId, string rowNumber)
        {
            string hashInputString = rawData + batchId + rowNumber;
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInputString));
            return Convert.ToHexString(hash);
        }
    }
}