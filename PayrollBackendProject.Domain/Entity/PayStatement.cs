using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Domain.Entity
{
    public class PayStatement
    {
        public Guid Id { get; private set; }

        // Keep the clinician for reference, but pull the snapshot information to make this immutable
        public Clinician Clinician { get; set; } = null!;
        public Guid ClinicianId { get; private set; }
        public decimal ClinicianCostShare { get; private set; }
        public List<PaymentSnapshot> LineItems { get; private set; } = new();
        public PayRun PayRun { get; set; } = null!;
        public Guid PayRunId { get; private set; }
        public decimal TotalPayment { get; private set; }
        public decimal CostShareAdjustedPayment { get; private set; }
        public decimal TotalAdjustment { get; private set; }
        public ApprovalStateEnum ApprovalState { get; private set; }
        public Guid? ApprovedRejectedBy { get; private set; }
        public DateTime? ApprovedRejectedOn { get; private set; }

        private PayStatement() { }
        private PayStatement(Clinician clinician, PayRun payRun, decimal costShare, ApprovalStateEnum status)
        {
            Id = Guid.NewGuid();
            Clinician = clinician;
            ClinicianId = clinician.ID;
            PayRun = payRun;
            ApprovalState = status;
            ClinicianCostShare = costShare;
            PayRunId = payRun.Id;
        }


        // Factory method
        public static PayStatement GenerateDraftPayStatement(Clinician clinician, PayRun payRun)
        {
            if(clinician == null || payRun == null)
            {
                throw new ArgumentException("Cannot generate a statement for a null clinician or pay run");
            }
            return new PayStatement(clinician, payRun, (decimal)clinician.CostShare, ApprovalStateEnum.DRAFT);
        }

        public void CalculateTotals()
        {
            if (ApprovalState != ApprovalStateEnum.DRAFT)
            {
                throw new InvalidOperationException("Can only compute totals in the statement draft state.");
            }
            TotalPayment = LineItems.Sum(p => p.PaymentAmount);
            TotalAdjustment = LineItems.Sum(p => p.AdjustmentAmount);
            CostShareAdjustedPayment = TotalPayment * ClinicianCostShare;
            ApprovalState = ApprovalStateEnum.PENDING;
        }

        public void Approve(UserAccount approver)
        {
            if (ApprovalState != ApprovalStateEnum.PENDING)
            {
                throw new InvalidOperationException("Can only approve pending statements.");
            }
            if (approver.Role != RoleEnum.ADMIN && approver.Role != RoleEnum.BACKEND)
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

        public void AddPaymentLineItem(PaymentSnapshot paymentSnapshot)
        {
            if (ApprovalState != ApprovalStateEnum.DRAFT)
            {
                throw new InvalidOperationException("Can only add line items in the statement draft state.");
            }
            if (paymentSnapshot.ClinicianId != ClinicianId)
            {
                throw new InvalidOperationException("Cannot add a payment line item where the clinician does not match the statement clinician ID");
            }
            this.LineItems.Add(paymentSnapshot);
        }

        public void EnsureEditable()
        {
            if (ApprovalState != ApprovalStateEnum.DRAFT && ApprovalState != ApprovalStateEnum.PENDING)
            {
                throw new InvalidOperationException("Cannot edit the pay statement in this approval state.");
            }
        }
    }
}
