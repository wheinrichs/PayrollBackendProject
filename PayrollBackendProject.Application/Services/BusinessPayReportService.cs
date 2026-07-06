using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Domain.Service;

namespace PayrollBackendProject.Application.Services
{
    public class BusinessPayReportService : IBusinessPayReportService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly PayrollCalculator _calculator;

        public BusinessPayReportService(
            IPaymentRepository paymentRepo,
            IClinicianRepository clinicianRepo,
            PayrollCalculator calculator)
        {
            _paymentRepo = paymentRepo;
            _clinicianRepo = clinicianRepo;
            _calculator = calculator;
        }

        public async Task<BusinessPayReportResponseDTO> GenerateReport(BusinessPayReportRequestDTO request)
        {
            if (request.IncludePsychTodayPayout && (!request.PsychTodayPayoutAmount.HasValue || request.PsychTodayPayoutAmount <= 0))
            {
                throw new ArgumentException("Psych Today payout amount must be greater than 0 when Psych Today payout is enabled.");
            }

            var (start, end) = BusinessPayReportMapper.DTOToDates(request);

            // Gather the automatically-flowing payment line items for the range. Code-500 (insurance takeback)
            // items never flow in automatically - they only enter a pay run via an explicit, itemized application,
            // so they're excluded here and reported separately from the historical lookup below.
            List<PaymentLineItem> payments = await _paymentRepo.GetPaymentBetweenDates(start, end);
            payments = payments.Where(p => p.PaymentAdjustmentCode != PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK).ToList();
            payments = payments.Where(p => p.ClinicianId != null).ToList();

            // Transient, never persisted: never passed to a repository Add* method or SaveChangesAsync.
            PayRun transientPayRun = PayRun.GeneratePayRun(start, end);
            List<PaymentSnapshot> snapshots = payments.Select(p => PaymentSnapshot.CreateSnapshot(p, transientPayRun)).ToList();
            transientPayRun.AssignPayments(snapshots);

            List<Clinician> clinicianList = await _clinicianRepo.GetAllClinicians();
            Dictionary<Guid, Clinician> clinicianMap = clinicianList.ToDictionary(c => c.ID, c => c);

            foreach (IGrouping<Guid, PaymentSnapshot> clinicianGroup in snapshots.GroupBy(p => p.ClinicianId!.Value))
            {
                Clinician clinician = clinicianMap[clinicianGroup.Key];
                PayStatement statement = _calculator.GeneratePayroll(
                    clinicianGroup.ToList(), clinician, transientPayRun,
                    request.IncludePsychTodayPayout, request.PsychTodayPayoutAmount ?? 0m);
                transientPayRun.Statements.Add(statement);
            }
            transientPayRun.CalculateTotals();

            // Historical code-500 deductions already applied via real, completed pay runs whose applied date
            // falls in this range - the report reflects settled history, it does not simulate new applications.
            List<Code500Application> code500Applications = await _paymentRepo.GetCode500ApplicationsBetweenDates(start, end);
            Dictionary<Guid, (decimal Total, int Count)> code500ByClinicianId = code500Applications
                .Where(a => a.PaymentLineItem.ClinicianId.HasValue)
                .GroupBy(a => a.PaymentLineItem.ClinicianId!.Value)
                .ToDictionary(g => g.Key, g => (Total: g.Sum(a => a.AppliedAmount), Count: g.Count()));

            return BusinessPayReportMapper.DomainToDTO(transientPayRun, code500ByClinicianId, clinicianMap);
        }
    }
}
