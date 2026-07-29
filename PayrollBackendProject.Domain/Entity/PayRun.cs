using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Domain.Entity
{
    public class PayRun
    {
        public Guid Id { get; private set; }
        public DateTime StartDate { get; set;}
        public DateTime EndDate { get; set;}
        public DateTime PaymentDate { get; private set; }
        public decimal TotalApplied { get; private set; } = 0.0m;
        public decimal TotalAdjudicated { get; private set; } = 0.0m;
        public decimal GrossPaymentTotal { get; private set; } = 0.0m;
        public decimal TotalCode500Deductions { get; private set; } = 0.0m;
        public List<PayStatement> Statements { get; private set; } = new();
        public List<PaymentSnapshot> Payments { get; private set; } = new();
        public Decimal StatementTotals { get; private set; }
        public decimal TotalPsychTodayPayout { get; private set; } = 0.0m;
        public decimal TotalPayout { get; private set; } = 0.0m;

        public PayRunStatusEnum GenerationStatus { get; set; } = PayRunStatusEnum.PENDING;
        public ApprovalStateEnum ApprovalState { get; private set; }
        public Guid? ApprovedRejectedBy { get; private set; }
        public DateTime? ApprovedRejectedOn { get; private set; }

        private PayRun() { }
        private PayRun(DateTime startDate, DateTime endDate, DateTime paymentDate, ApprovalStateEnum status)
        {
            Id = Guid.NewGuid();
            StartDate = startDate;
            EndDate = endDate;
            PaymentDate = paymentDate;
            ApprovalState = status;
        }

        // Factory method
        public static PayRun GeneratePayRun(DateTime startDate, DateTime endDate, DateTime? paymentDate = null)
        {
            if(endDate.Date > DateTime.UtcNow.Date)
            {
                throw new ArgumentException("Cannot set a pay run through a day in the future.");
            }
            return new PayRun(startDate, endDate, paymentDate ?? NextDefaultPaymentDate(DateTime.UtcNow), ApprovalStateEnum.DRAFT);
        }

        /// <summary>
        /// The default payout date: the next 1st or 15th of the month, whichever comes first strictly after the given date.
        /// </summary>
        public static DateTime NextDefaultPaymentDate(DateTime fromDate)
        {
            DateTime day = fromDate.Date;
            DateTime candidate = day.AddDays(1);
            while (candidate.Day != 1 && candidate.Day != 15)
            {
                candidate = candidate.AddDays(1);
            }
            return DateTime.SpecifyKind(candidate, DateTimeKind.Utc);
        }

        public void AssignPayments(List<PaymentSnapshot> payments)
        {
            if (ApprovalState != ApprovalStateEnum.DRAFT)
            {
                throw new InvalidOperationException("Can only compute totals in pay run draft state");
            }
            if (Payments.Any())
            {
                throw new InvalidOperationException("Cannot reassign payments");
            }
            Payments = payments;
        }

        public void CalculateTotals()
        {
            if(ApprovalState != ApprovalStateEnum.DRAFT)
            {
                throw new InvalidOperationException("Can only compute totals in pay run draft state");
            }
            GrossPaymentTotal = Statements.Sum(s => s.TotalPayment);
            TotalAdjudicated = Payments.Sum(p => p.AdjustmentAmount);
            TotalApplied = Payments.Sum(p => p.PaymentAmount);
            TotalCode500Deductions = Math.Abs(Payments
                .Where(p => p.AdjustmentCode == PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK)
                .Sum(p => p.AdjustmentAmount));
            StatementTotals = Statements.Sum(s => s.CostShareAdjustedPayment);
            TotalPsychTodayPayout = Statements.Sum(s => s.PsychTodayPayout);
            TotalPayout = StatementTotals + TotalPsychTodayPayout;
            ApprovalState = ApprovalStateEnum.PENDING;
            GenerationStatus = PayRunStatusEnum.COMPLETED;
        }

        public void Approve(UserAccount approver)
        {
            if(ApprovalState != ApprovalStateEnum.PENDING)
            {
                throw new InvalidOperationException("Can only approve pending pay runs.");
            }
            if(approver.Role != RoleEnum.ADMIN && approver.Role != RoleEnum.BACKEND)
            {
                throw new InvalidOperationException("Can only be approved by backend or admin user.");
            }
            ApprovalState = ApprovalStateEnum.APPROVED;
            ApprovedRejectedBy = approver.Id;
            ApprovedRejectedOn = DateTime.UtcNow;
        }

        public void Reject(UserAccount approver)
        {
            if (approver.Role != RoleEnum.ADMIN && approver.Role != RoleEnum.BACKEND)
            {
                throw new InvalidOperationException("Can only be rejected by backend or admin user.");
            }
            ApprovalState = ApprovalStateEnum.REJECTED;
            ApprovedRejectedBy = approver.Id;
            ApprovedRejectedOn = DateTime.UtcNow;
        }

        public void EnsureEditable()
        {
            if(ApprovalState != ApprovalStateEnum.DRAFT && ApprovalState != ApprovalStateEnum.PENDING)
            {
                throw new InvalidOperationException("Cannot edit the pay run in this approval state.");
            }
        }
    }
}
