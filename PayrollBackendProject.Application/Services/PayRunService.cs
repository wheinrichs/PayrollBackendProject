using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Domain.Service;
using System.Text.Json;

namespace PayrollBackendProject.Application.Services
{
    public class PayRunService : IPayRunService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IUnitOfWork _unitOfWorkRepo;
        private readonly PayrollCalculator _calculator;
        private readonly IPayRunRepository _payRunRepo;
        private readonly IPayStatementRepository _payStatementRepo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly IUserAccountRepository _userAccountRepo;
        private readonly IAuditLogRepository _auditLogRepo;

        public PayRunService(IPaymentRepository paymentRepo, 
            IUnitOfWork unitOfWorkRepo, 
            PayrollCalculator payrollCalculator, 
            IPayRunRepository payRunRepo, 
            IPayStatementRepository payStatementRepo, 
            IClinicianRepository clinicianRepo, 
            IUserAccountRepository userAccountRepo, 
            IAuditLogRepository auditLogRepository)
        {
            _paymentRepo = paymentRepo;
            _unitOfWorkRepo = unitOfWorkRepo;
            _calculator = payrollCalculator;
            _payRunRepo = payRunRepo;
            _payStatementRepo = payStatementRepo;
            _clinicianRepo = clinicianRepo;
            _userAccountRepo = userAccountRepo;
            _auditLogRepo = auditLogRepository;
        }
         
        public async Task<PayRunResponseDTO> ExecutePayRun(PayRunRequestDTO request, Guid userId)
        {
            if (request.IncludePsychTodayPayout && (!request.PsychTodayPayoutAmount.HasValue || request.PsychTodayPayoutAmount <= 0))
            {
                throw new ArgumentException("Psych Today payout amount must be greater than 0 when Psych Today payout is enabled.");
            }

            var (start, end) = PayRunMapper.DTOToDates(request);

            List<PayRun> overlapping = await _payRunRepo.GetOverlappingPayRuns(start, end);
            if (overlapping.Any())
            {
                throw new ArgumentException(
                    $"Pay run date range {start:yyyy-MM-dd} - {end:yyyy-MM-dd} overlaps an existing pay run.");
            }

            // Gather all the payment line items, create a snapshot for each one, and assign them to a new pay run.
            // 500-code (INSURANCE_TAKEBACK) line items never flow in automatically - they only enter a pay run
            // when explicitly applied (fully or partially) via request.Code500Applications below.
            List<PaymentLineItem> payments = await _paymentRepo.GetPaymentBetweenDates(start, end);
            payments = payments.Where(p =>
                p.PaymentAdjustmentCode != Domain.Enums.PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK
            ).ToList();
            // TODO think about if this is the right behavior - right now you are filtering out payments that are in the system but have no clinician entity - is this right? Do we want to include these in the payrun?
            payments = payments.Where(p => p.ClinicianId != null).ToList();
            PayRun payRun = PayRun.GeneratePayRun(start, end);
            List<PaymentSnapshot> snapshotPayments = payments.Select(p => PaymentSnapshot.CreateSnapshot(p, payRun)).ToList();

            // Apply the requested portion (full or partial) of each outstanding 500-code balance to this pay run
            foreach (Code500ApplicationRequestDTO applyRequest in request.Code500Applications)
            {
                PaymentLineItem? lineItem = await _paymentRepo.GetPaymentLineItemById(applyRequest.PaymentLineItemId);
                if (lineItem == null)
                    throw new KeyNotFoundException($"Payment line item {applyRequest.PaymentLineItemId} not found.");

                Code500Application application = lineItem.ApplyCode500(applyRequest.Amount, userId, payRun);
                _paymentRepo.AddCode500Application(application);
                snapshotPayments.Add(PaymentSnapshot.CreateSnapshot(application, lineItem, payRun));
            }

            payRun.AssignPayments(snapshotPayments);

            // Log pay run create
            string oldLogState = "";
            string newLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));
            AuditLog createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.CREATED, oldLogState, newLogState, userId.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            // Retrieve all the clinicians
            List<Clinician> clinicianList = await _clinicianRepo.GetAllClinicians();
            Dictionary<Guid, Clinician> clinicianMap = clinicianList.ToDictionary(c => c.ID, c => c);

            // Group all the snapshots by the clinician
            IEnumerable<IGrouping<Guid, PaymentSnapshot>> clinicians = snapshotPayments.GroupBy(p => p.ClinicianId!.Value);

            // Process each payment for each clinician
            payRun.GenerationStatus = Domain.Enums.PayRunStatusEnum.PROCESSING;

            // Log pay run as processing
            oldLogState = newLogState;
            newLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));
            createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.UPDATED, oldLogState, newLogState, userId.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            foreach (var clinicianGroup in clinicians)
            {
                Guid clinicianId = clinicianGroup.Key;
                Clinician clinician = clinicianMap[clinicianId];

                // Generate a statement for each clinician
                PayStatement statement = _calculator.GeneratePayroll(
                    clinicianGroup.ToList(), clinician, payRun,
                    request.IncludePsychTodayPayout, request.PsychTodayPayoutAmount ?? 0m);

                // Log the created pay statement
                string newStatementLogState = JsonSerializer.Serialize(PayStatementMapper.DomainToDTO(statement, statement.PayRunId));
                AuditLog createdStatementLog = new("Pay Statement", statement.Id, AuditLogActionEnum.CREATED, "", newStatementLogState, userId.ToString());
                await _auditLogRepo.AddAuditLog(createdStatementLog);

                // Add the statement to the pay run
                payRun.Statements.Add(statement);
                //_payStatementRepo.AddStatement(statement);`
            }
            payRun.CalculateTotals();

            // Log the pay run as pending approval
            oldLogState = newLogState;
            newLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));
            createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.UPDATED, oldLogState, newLogState, userId.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            _payRunRepo.AddPayRun(payRun);
            await _unitOfWorkRepo.SaveChangesAsync();

            return PayRunMapper.DomainToDTO(payRun);

        }

        public async Task<List<PayRunResponseDTO>> GetAllPayRuns()
        {
            List<PayRun> payRuns = await _payRunRepo.GetAllPayRuns();
            return payRuns.Select(PayRunMapper.DomainToDTO).ToList();
        }

        public async Task<List<PayStatementDTO>> RetrievePayStatementsForRun(Guid PayRunGuid)
        {
            List<PayStatement> statements = await _payStatementRepo.GetPayStatementsForPayRun(PayRunGuid);
            List<PayStatementDTO> result = statements.Select(s=> PayStatementMapper.DomainToDTO(s, PayRunGuid)).ToList();
            return result;
        }

        public async Task ApprovePayRun(Guid payRunGuid, Guid approverGuid)
        {
            PayRun? payRun = await _payRunRepo.GetPayRun(payRunGuid);
            UserAccount? approver = await _userAccountRepo.GetById(approverGuid);
            if (payRun == null)
            {
                throw new KeyNotFoundException("Pay run does not exist in database.");
            }
            if (approver == null)
            {
                throw new KeyNotFoundException("Approver does not exist as User in database.");
            }
            string oldLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));

            payRun.Approve(approver);

            // Log the approval
            string newLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));
            AuditLog createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.APPROVED, oldLogState, newLogState, approverGuid.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            await _unitOfWorkRepo.SaveChangesAsync();
        }

        public async Task ApprovePayStatement(Guid PayStatementGuid, Guid approverGuid)
        {
            PayStatement? payStatement = await _payStatementRepo.GetPayStatement(PayStatementGuid);
            UserAccount? approver = await _userAccountRepo.GetById(approverGuid);
            if (payStatement == null)
            {
                throw new KeyNotFoundException("Pay statement does not exist in database.");
            }
            if (approver == null)
            {
                throw new KeyNotFoundException("Approver does not exist as User in database.");
            }
            string oldLogState = JsonSerializer.Serialize(PayStatementMapper.DomainToDTO(payStatement, payStatement.PayRunId));

            payStatement.Approve(approver);

            // Log the approved pay statement
            string newLogState = JsonSerializer.Serialize(PayStatementMapper.DomainToDTO(payStatement, payStatement.PayRunId));
            AuditLog createdLog = new("Pay Statement", payStatement.Id, AuditLogActionEnum.APPROVED, oldLogState, newLogState, approverGuid.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            await _unitOfWorkRepo.SaveChangesAsync();
        }

        public async Task RejectPayRun(Guid payRunGuid, Guid approverGuid)
        {
            PayRun? payRun = await _payRunRepo.GetPayRun(payRunGuid);
            UserAccount? approver = await _userAccountRepo.GetById(approverGuid);
            if (payRun == null)
            {
                throw new KeyNotFoundException("Pay run does not exist in database.");
            }
            if (approver == null)
            {
                throw new KeyNotFoundException("Approver does not exist as User in database.");
            }
            string oldLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));

            payRun.Reject(approver);

            // Log the rejection
            string newLogState = JsonSerializer.Serialize(PayRunMapper.DomainToDTO(payRun));
            AuditLog createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.REJECTED, oldLogState, newLogState, approverGuid.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            await _unitOfWorkRepo.SaveChangesAsync();
        }

        public async Task RejectPayStatement(Guid PayStatementGuid, Guid approverGuid)
        {
            PayStatement? payStatement = await _payStatementRepo.GetPayStatement(PayStatementGuid);
            UserAccount? approver = await _userAccountRepo.GetById(approverGuid);
            if (payStatement == null)
            {
                throw new KeyNotFoundException("Pay statement does not exist in database.");
            }
            if (approver == null)
            {
                throw new KeyNotFoundException("Approver does not exist as User in database.");
            }
            string oldLogState = JsonSerializer.Serialize(PayStatementMapper.DomainToDTO(payStatement, payStatement.PayRunId));

            payStatement.Reject(approver);

            // Log the rejected pay statement
            string newLogState = JsonSerializer.Serialize(PayStatementMapper.DomainToDTO(payStatement, payStatement.PayRunId));
            AuditLog createdLog = new("Pay Statement", payStatement.Id, AuditLogActionEnum.REJECTED, oldLogState, newLogState, approverGuid.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);

            await _unitOfWorkRepo.SaveChangesAsync();
        }

        public async Task<List<PayStatementDTO>> RetrieveStatementsForUser(Guid userId)
        {
            // Get the clinician ID from the user account ID
            UserAccount? userAccount = await _userAccountRepo.GetById(userId);
            if (userAccount == null)
            {
                throw new Exception($"User with ID {userId} not found");
            }
            if (userAccount.ClinicianId == null)
            {
                throw new InvalidOperationException("User is not a clinician");
            }
            List<PayStatement> result = await _payStatementRepo.GetPayStatementsForUser(userAccount.ClinicianId.Value);
            // Only return approved statements for the user
            List<PayStatementDTO> dtoResult = result
                .Where(s => s.ApprovalState == ApprovalStateEnum.APPROVED)
                .Select(s => PayStatementMapper.DomainToDTO(s, s.PayRunId))
                .ToList();
            return dtoResult;
        }
    }
}
