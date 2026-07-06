using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using System.Text.Json;

namespace PayrollBackendProject.Application.Services
{
    public class PaymentLineItemService : IPaymentLineItemService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly IEHRUserAccountRepository _ehrUserRepo;
        private readonly IUserAccountRepository _userAccountRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IFingerprintGenerator _fingerprintGenerator;

        public PaymentLineItemService(
            IPaymentRepository paymentRepo,
            IClinicianRepository clinicianRepo,
            IEHRUserAccountRepository ehrUserRepo,
            IUserAccountRepository userAccountRepo,
            IUnitOfWork unitOfWork,
            IAuditLogRepository auditLogRepo,
            IFingerprintGenerator fingerprintGenerator)
        {
            _paymentRepo = paymentRepo;
            _clinicianRepo = clinicianRepo;
            _ehrUserRepo = ehrUserRepo;
            _userAccountRepo = userAccountRepo;
            _unitOfWork = unitOfWork;
            _auditLogRepo = auditLogRepo;
            _fingerprintGenerator = fingerprintGenerator;
        }

        public async Task<List<UnappliedCode500ResponseDTO>> GetUnappliedCode500Payments()
        {
            List<PaymentLineItem> items = await _paymentRepo.GetUnappliedCode500Payments();
            return items.Select(PaymentLineItemMapper.DomainToUnappliedCode500DTO).ToList();
        }

        public async Task<Guid> AddManualPayment(ManualPaymentRequestDTO dto, Guid userId)
        {
            if (!Enum.IsDefined(typeof(PaymentAdjustmentCodeEnum), dto.PaymentAdjustmentCode))
                throw new ArgumentException($"Invalid adjustment code: {dto.PaymentAdjustmentCode}");

            string fingerprint = await _fingerprintGenerator.ManualEntryComputeSHA256Async(
                dto.PatientId, dto.CPTCode, dto.PaymentId ?? string.Empty, dto.DateOfService, dto.PaymentAdjustmentCode);

            PaymentLineItem? existing = await _paymentRepo.GetPaymentLineItem(fingerprint);
            if (existing != null)
                throw new InvalidOperationException("A payment with the same identifying details already exists.");

            Clinician? clinician = null;
            if (dto.ClinicianId.HasValue)
            {
                clinician = await _clinicianRepo.GetClinicianByID(dto.ClinicianId.Value);
                if (clinician == null)
                    throw new KeyNotFoundException($"Clinician with ID {dto.ClinicianId.Value} not found.");
            }

            EHRUser appliedBy = await ResolveAppliedByUser(userId);

            PaymentLineItem item = PaymentLineItemMapper.ManualDtoToDomain(dto, clinician, appliedBy, fingerprint);

            await _paymentRepo.AddLineItem(item);

            string newState = JsonSerializer.Serialize(PaymentLineItemMapper.DomainToDto(item));
            AuditLog log = new("Payment Line Item", item.Id, AuditLogActionEnum.CREATED, "", newState, userId.ToString());
            await _auditLogRepo.AddAuditLog(log);

            await _unitOfWork.SaveChangesAsync();
            return item.Id;
        }

        private async Task<EHRUser> ResolveAppliedByUser(Guid userId)
        {
            UserAccount? userAccount = await _userAccountRepo.GetById(userId);
            if (userAccount == null)
                throw new KeyNotFoundException($"User account with ID {userId} not found.");

            EHRUser? ehrUser = await _ehrUserRepo.GetUserByUsername(userAccount.Email);
            if (ehrUser != null)
                return ehrUser;

            EHRUser newUser = new EHRUser(string.Empty, string.Empty, userAccount.Email);
            _ehrUserRepo.AddNewUser(newUser);
            return newUser;
        }
    }
}
