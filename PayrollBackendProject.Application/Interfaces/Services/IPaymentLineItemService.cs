using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IPaymentLineItemService
    {
        Task<List<UnappliedCode500ResponseDTO>> GetUnappliedCode500Payments();
        Task<List<UnappliedCode500ResponseDTO>> GetRejectedCode500Payments();
        Task<Guid> AddManualPayment(ManualPaymentRequestDTO dto, Guid userId);
        Task RejectCode500Payment(Guid id, Guid userId);
    }
}
