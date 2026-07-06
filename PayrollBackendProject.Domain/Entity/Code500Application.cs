namespace PayrollBackendProject.Domain.Entity
{
    public class Code500Application
    {
        public Guid Id { get; private set; }

        public Guid PaymentLineItemId { get; private set; }
        public PaymentLineItem PaymentLineItem { get; private set; } = null!;

        public decimal AppliedAmount { get; private set; }

        public DateTime AppliedDate { get; private set; }

        public Guid AppliedByUserId { get; private set; }

        public Guid PayRunId { get; private set; }
        public PayRun PayRun { get; private set; } = null!;

        private Code500Application() { }

        private Code500Application(PaymentLineItem paymentLineItem, decimal appliedAmount, Guid appliedByUserId, PayRun payRun)
        {
            Id = Guid.NewGuid();
            PaymentLineItemId = paymentLineItem.Id;
            PaymentLineItem = paymentLineItem;
            AppliedAmount = appliedAmount;
            AppliedDate = payRun.EndDate;
            AppliedByUserId = appliedByUserId;
            PayRunId = payRun.Id;
            PayRun = payRun;
        }

        public static Code500Application Create(PaymentLineItem paymentLineItem, decimal appliedAmount, Guid appliedByUserId, PayRun payRun)
        {
            if (paymentLineItem == null) throw new ArgumentNullException(nameof(paymentLineItem));
            if (payRun == null) throw new ArgumentNullException(nameof(payRun));
            if (appliedAmount <= 0) throw new ArgumentException("Applied amount must be greater than 0.");

            return new Code500Application(paymentLineItem, appliedAmount, appliedByUserId, payRun);
        }
    }
}
