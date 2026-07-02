using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Domain.Service
{
    public class PayrollCalculator
    {
        public PayStatement GeneratePayroll(List<PaymentSnapshot> payments, Clinician clinician, PayRun payRun, bool includePsychTodayPayout, decimal psychTodayPayoutAmount)
        {
            PayStatement statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);
            foreach (PaymentSnapshot item in payments)
            {
                statement.AddPaymentLineItem(item);
                item.AssignStatement(statement);
            }
            if (includePsychTodayPayout && clinician.HasPsychToday)
            {
                statement.ApplyPsychTodayPayout(psychTodayPayoutAmount);
            }
            statement.CalculateTotals();
            return statement;
        }
    }
}
