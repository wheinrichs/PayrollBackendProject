using PayrollBackendProject.Domain.Enums;
using System.Net;

namespace PayrollBackendProject.Domain.Entity
{
    public class PayRun
    {
        public Guid Id { get; private set; }
        public DateTime StartDate { get; set;}
        public DateTime EndDate { get; set;}
        public Decimal TotalApplied { get; private set; } = 0.0m;
        public Decimal TotalAdjudicated { get; private set; } = 0.0m;
        public List<PayStatement> Statements { get; private set; } = new();
        public List<PaymentSnapshot> Payments { get; private set; } = new();
        public Decimal StatementTotals { get; private set; }

        public PayRunStatusEnum GenerationStatus { get; set; } = PayRunStatusEnum.PENDING;
        public ApprovalStateEnum ApprovalState { get; private set; }
        public Guid? ApprovedRejectedBy { get; private set; }
        public DateTime? ApprovedRejectedOn { get; private set; }

        private PayRun() { }
        private PayRun(DateTime startDate, DateTime endDate, ApprovalStateEnum status)
        {
            Id = Guid.NewGuid();
            StartDate = startDate;
            EndDate = endDate;
            ApprovalState = status;
        }

        // Factory method
        public static PayRun GeneratePayRun(DateTime startDate, DateTime endDate)
        {
            return new PayRun(startDate, endDate, ApprovalStateEnum.DRAFT);
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
            StatementTotals = Statements.Sum(s => s.TotalPayment);
            TotalAdjudicated = Payments.Sum(p => p.AdjustmentAmount);
            TotalApplied = Payments.Sum(p => p.PaymentAmount);
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
                throw new InvalidOperationException("Can only be approved by backend or admin user.");
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
