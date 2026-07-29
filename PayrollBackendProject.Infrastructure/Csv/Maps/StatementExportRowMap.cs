using CsvHelper.Configuration;
using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Mappings
{
    public sealed class StatementExportRowMap : ClassMap<StatementExportRowDTO>
    {
        public StatementExportRowMap()
        {
            Map(m => m.PayRunId);
            Map(m => m.PayRunStartDate).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.PayRunEndDate).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.PayRunPaymentDate).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.PayStatementId);
            Map(m => m.ClinicianId);
            Map(m => m.ClinicianFirstName);
            Map(m => m.ClinicianLastName);
            Map(m => m.ClinicianEmail);
            Map(m => m.CostShareSnapshot).TypeConverterOption.Format("F4");
            Map(m => m.TotalPayment).TypeConverterOption.Format("F2");
            Map(m => m.TotalAdjustment).TypeConverterOption.Format("F2");
            Map(m => m.Code500Deductions).TypeConverterOption.Format("F2");
            Map(m => m.CostShareAdjustedPayment).TypeConverterOption.Format("F2");
            Map(m => m.PsychTodayPayout).TypeConverterOption.Format("F2");
            Map(m => m.TotalPayout).TypeConverterOption.Format("F2");
            Map(m => m.StatementApprovedOn).TypeConverterOption.Format("yyyy-MM-dd HH:mm:ss");
            Map(m => m.LineItemId);
            Map(m => m.PatientId);
            Map(m => m.DateOfService).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.CPTCode);
            Map(m => m.PaymentId);
            Map(m => m.Payer);
            Map(m => m.AdjustmentCode);
            Map(m => m.AdjustmentCodeName);
            Map(m => m.PaymentAmount).TypeConverterOption.Format("F2");
            Map(m => m.AdjustmentAmount).TypeConverterOption.Format("F2");
            Map(m => m.AppliedDate).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.PaymentDate).TypeConverterOption.Format("yyyy-MM-dd");
            Map(m => m.RowNumber);
        }
    }
}
