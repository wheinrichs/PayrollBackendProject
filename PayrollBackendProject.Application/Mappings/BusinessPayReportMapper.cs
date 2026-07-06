using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class BusinessPayReportMapper
    {
        public static (DateTime, DateTime) DTOToDates(BusinessPayReportRequestDTO dto)
        {
            return (
                DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc),
                DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc)
            );
        }

        public static BusinessPayReportResponseDTO DomainToDTO(
            PayRun transientPayRun,
            Dictionary<Guid, (decimal Total, int Count)> code500ByClinicianId,
            Dictionary<Guid, Clinician> clinicianMap)
        {
            Dictionary<Guid, PayStatement> statementsByClinicianId = transientPayRun.Statements
                .ToDictionary(s => s.ClinicianId);

            IEnumerable<Guid> allClinicianIds = statementsByClinicianId.Keys.Union(code500ByClinicianId.Keys);

            List<ClinicianPayReportEntryDTO> entries = allClinicianIds.Select(clinicianId =>
            {
                statementsByClinicianId.TryGetValue(clinicianId, out PayStatement? statement);
                code500ByClinicianId.TryGetValue(clinicianId, out (decimal Total, int Count) code500);
                Clinician clinician = clinicianMap[clinicianId];

                return new ClinicianPayReportEntryDTO(
                    clinicianId,
                    clinician.FirstName,
                    clinician.LastName,
                    statement?.TotalPayment ?? 0m,
                    statement?.TotalAdjustment ?? 0m,
                    code500.Total,
                    code500.Count,
                    statement?.CostShareAdjustedPayment ?? 0m,
                    statement?.PsychTodayPayout ?? 0m,
                    statement?.TotalPayout ?? 0m,
                    statement?.ClinicianCostShare ?? (decimal)clinician.CostShare);
            })
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToList();

            decimal totalCode500Deductions = code500ByClinicianId.Values.Sum(v => v.Total);
            int totalCode500LineItemCount = code500ByClinicianId.Values.Sum(v => v.Count);

            return new BusinessPayReportResponseDTO(
                transientPayRun.StartDate,
                transientPayRun.EndDate,
                transientPayRun.TotalApplied,
                transientPayRun.TotalAdjudicated,
                transientPayRun.GrossPaymentTotal,
                totalCode500Deductions,
                totalCode500LineItemCount,
                transientPayRun.StatementTotals,
                transientPayRun.TotalPsychTodayPayout,
                transientPayRun.TotalPayout,
                entries);
        }
    }
}
