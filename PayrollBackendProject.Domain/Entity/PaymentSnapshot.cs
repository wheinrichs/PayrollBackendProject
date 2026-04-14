using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Domain.Entity
{
    public class PaymentSnapshot
    {
        // This class represents a snapshot of a payment line item. This protects against payments being deleted
        // This class is used in payruns and pay statements as an immutable entry for the payment
        // Each snapshot is associated with one pay run and one pay statement as no payments can be on multiple Clinicians statements
        public Guid? PayStatementId { get; private set; }
        public PayStatement? PayStatement { get; private set; }
        public Guid PayRunId { get; private set; }
        public PayRun PayRun { get; private set; } = null!;
        // This is the guid of the source data. It is just here for auditing if something goes wrong in the future
        public Guid PaymentLineItemId { get; private set; }

        // Below are the relevant snap shot fields
        public Guid Id { get; private set; }
        public string RawData { get; private set; } = string.Empty;
        public Guid? ClinicianId { get; private set; }
        public decimal PaymentAmount { get; private set; }
        public decimal AdjustmentAmount { get; private set; }
        public AdjustmentCodeEnum AdjustmentCode { get; private set; }
        public DateTime DateOfService { get; private set; }
        public string PatientId { get; private set; } = string.Empty;
        public string CPTCode { get; private set; } = string.Empty;
        public string PaymentId { get; private set; } = string.Empty;
        public string Payer { get; private set; } = string.Empty;
        public Guid AppliedById { get; private set; }
        public Guid ImportBatchId { get; private set; }
        public int RowNumber { get; private set; }
        public DateTime AppliedDate { get; private set; }
        public DateTime? PaymentDate { get; private set; }

        public PaymentSnapshot() { }

        private PaymentSnapshot(PaymentLineItem paymentLineItem,
                               PayRun payRun
                               )
        {
            Id = Guid.NewGuid();
            // Save the source info
            PayRun = payRun;
            PayRunId = payRun.Id;
            PaymentLineItemId = paymentLineItem.Id;

            // Save the snapshot information
            RawData = paymentLineItem.RawData;
            ClinicianId = paymentLineItem.ClinicianId;
            PaymentAmount = paymentLineItem.PaymentAmount;
            AdjustmentAmount = paymentLineItem.AdjustmentAmount;
            AdjustmentCode = paymentLineItem.AdjustmentCode;
            DateOfService = paymentLineItem.DateOfService;
            PatientId = paymentLineItem.PatientId;
            CPTCode = paymentLineItem.CPTCode;
            PaymentId = paymentLineItem.PaymentId;
            Payer = paymentLineItem.Payer;
            AppliedById = paymentLineItem.AppliedById;
            ImportBatchId = paymentLineItem.ImportBatchId;
            RowNumber = paymentLineItem.RowNumber;
            AppliedDate = paymentLineItem.AppliedDate;
            PaymentDate = paymentLineItem.PaymentDate;
        }

        public static PaymentSnapshot CreateSnapshot(PaymentLineItem paymentLineItem, PayRun payRun)
        {
            return new PaymentSnapshot(paymentLineItem, payRun);
        }

        public void AssignStatement(PayStatement statement)
        {
            if (PayStatement != null)
                throw new InvalidOperationException("Snapshot already assigned.");

            PayStatement = statement;
            PayStatementId = statement.Id;

        }
    }
}
