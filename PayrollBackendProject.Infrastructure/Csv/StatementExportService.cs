using CsvHelper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PayrollBackendProject.Application.Services
{
    public class StatementExportService : IStatementExportService
    {
        private readonly IPayRunRepository _payRunRepo;
        private readonly IPayStatementRepository _payStatementRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;

        public StatementExportService(IPayRunRepository payRunRepo,
            IPayStatementRepository payStatementRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork)
        {
            _payRunRepo = payRunRepo;
            _payStatementRepo = payStatementRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<StatementExportResultDTO> ExportApprovedStatementsCsv(Guid payRunGuid, Guid userId)
        {
            PayRun? payRun = await _payRunRepo.GetPayRun(payRunGuid);
            if (payRun == null)
            {
                throw new KeyNotFoundException("Pay run not found");
            }

            // Only approved statements are exported; pending/rejected statements are excluded
            List<PayStatement> statements = await _payStatementRepo.GetPayStatementsForPayRun(payRunGuid);
            List<PayStatement> approvedStatements = statements
                .Where(s => s.ApprovalState == ApprovalStateEnum.APPROVED)
                .OrderBy(s => s.Clinician.LastName)
                .ThenBy(s => s.Clinician.FirstName)
                .ToList();

            if (approvedStatements.Count == 0)
            {
                throw new InvalidOperationException("Pay run has no approved statements to export.");
            }

            List<StatementExportRowDTO> rows = FlattenStatements(payRun, approvedStatements);

            byte[] content = WriteCsv(rows);
            string fileName = $"approved-statements_{payRun.StartDate:yyyyMMdd}-{payRun.EndDate:yyyyMMdd}.csv";

            // Exports are a data egress event so record them in the audit log
            string exportLogState = JsonSerializer.Serialize(new
            {
                StatementCount = approvedStatements.Count,
                RowCount = rows.Count,
                FileName = fileName
            });
            AuditLog createdLog = new("Pay Run", payRun.Id, AuditLogActionEnum.EXPORTED, "", exportLogState, userId.ToString());
            await _auditLogRepo.AddAuditLog(createdLog);
            await _unitOfWork.SaveChangesAsync();

            return new StatementExportResultDTO
            {
                Content = content,
                FileName = fileName,
                StatementCount = approvedStatements.Count,
                RowCount = rows.Count
            };
        }

        private static List<StatementExportRowDTO> FlattenStatements(PayRun payRun, List<PayStatement> statements)
        {
            List<StatementExportRowDTO> rows = new();
            foreach (PayStatement statement in statements)
            {
                IEnumerable<PaymentSnapshot> orderedLineItems = statement.LineItems
                    .OrderBy(li => li.DateOfService)
                    .ThenBy(li => li.RowNumber);

                foreach (PaymentSnapshot lineItem in orderedLineItems)
                {
                    rows.Add(new StatementExportRowDTO
                    {
                        PayRunId = payRun.Id,
                        PayRunStartDate = payRun.StartDate,
                        PayRunEndDate = payRun.EndDate,
                        PayRunPaymentDate = payRun.PaymentDate,
                        PayStatementId = statement.Id,
                        ClinicianId = statement.ClinicianId,
                        ClinicianFirstName = statement.Clinician.FirstName,
                        ClinicianLastName = statement.Clinician.LastName,
                        ClinicianEmail = statement.Clinician.Email,
                        CostShareSnapshot = statement.ClinicianCostShare,
                        TotalPayment = statement.TotalPayment,
                        TotalAdjustment = statement.TotalAdjustment,
                        Code500Deductions = statement.Code500Deductions,
                        CostShareAdjustedPayment = statement.CostShareAdjustedPayment,
                        PsychTodayPayout = statement.PsychTodayPayout,
                        TotalPayout = statement.TotalPayout,
                        StatementApprovedOn = statement.ApprovedRejectedOn,
                        LineItemId = lineItem.Id,
                        PatientId = lineItem.PatientId,
                        DateOfService = lineItem.DateOfService,
                        CPTCode = lineItem.CPTCode,
                        PaymentId = lineItem.PaymentId,
                        Payer = lineItem.Payer,
                        AdjustmentCode = (int)lineItem.AdjustmentCode,
                        AdjustmentCodeName = lineItem.AdjustmentCode.ToString(),
                        PaymentAmount = lineItem.PaymentAmount,
                        AdjustmentAmount = lineItem.AdjustmentAmount,
                        AppliedDate = lineItem.AppliedDate,
                        PaymentDate = lineItem.PaymentDate,
                        RowNumber = lineItem.RowNumber
                    });
                }
            }
            return rows;
        }

        private static byte[] WriteCsv(List<StatementExportRowDTO> rows)
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(false)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<StatementExportRowMap>();
                csv.WriteRecords(rows);
            }
            return memoryStream.ToArray();
        }
    }
}
