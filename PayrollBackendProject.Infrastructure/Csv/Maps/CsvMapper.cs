using CsvHelper.Configuration;
using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Mappings
{
    public sealed class CsvMapper : ClassMap<PaymentCsvRow>
    {
        public CsvMapper() {
            Map(m => m.ClinicianName).Name("Provider Name");
            Map(m => m.AppliedDate).Name("Applied Date");
            Map(m => m.PatientID).Name("Patient ID");
            Map(m => m.DOS).Name("DOS");
            Map(m => m.CPT).Name("CPT");
            Map(m => m.PaymentID).Name("Payment ID");
            Map(m => m.PaymentDate).Name("Payment Date");
            Map(m => m.AppliedPayments).Name("Applied Payments");
            Map(m => m.AppliedAdjustment).Name("Applied Adjustments");
            Map(m => m.Desc).Name("Desc");
            Map(m => m.AcctNo).Name("Acct no");
            Map(m => m.Payer).Name("Payer");
            Map(m => m.AppliedBy).Name("Applied By");
        }
    }
}
