using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IBusinessPayReportService
    {
        public Task<BusinessPayReportResponseDTO> GenerateReport(BusinessPayReportRequestDTO request);
    }
}
