using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using System.Threading.Tasks.Dataflow;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface ICsvParserService
    {
        public Task<UploadResult?> Parse(ImportBatch batch);
        public Task<ClinicianMatchResult> MatchUnresolvedPaymentLineItems();
    }
}
