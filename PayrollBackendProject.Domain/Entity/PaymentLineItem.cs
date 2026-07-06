using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Domain.Entity
{
    public class PaymentLineItem
    {
        public Guid Id { get; private set; }

        public string RawData { get; private set; } = string.Empty;

        public Clinician? Clinician { get; private set; }
        public Guid? ClinicianId { get; private set; }
        public string RawClinicianName { get; private set; } = string.Empty;

        public decimal PaymentAmount { get; private set; }
        public decimal AdjustmentAmount { get; private set; }
        public PaymentAdjustmentCodeEnum PaymentAdjustmentCode { get; private set; }

        public DateTime DateOfService { get; private set; }

        public string PatientId { get; private set; } = string.Empty;
        public string CPTCode { get; private set; } = string.Empty;
        public string PaymentId { get; private set; } = string.Empty;
        public string Payer { get; private set; } = string.Empty;

        public EHRUser AppliedBy { get; private set; } = null!;
        public Guid AppliedById { get; private set; }

        public ImportBatch? ImportBatch { get; private set; }
        public Guid? ImportBatchId { get; private set; }

        public bool IsCode500Applied { get; private set; }

        public decimal Code500AppliedAmount { get; private set; }
        public List<Code500Application> Code500Applications { get; private set; } = new();

        public decimal RemainingCode500Amount => Math.Abs(AdjustmentAmount) - Code500AppliedAmount;

        public string Fingerprint { get; private set; } = string.Empty;

        public int RowNumber { get; private set; }

        public DateTime AppliedDate { get; private set; }
        public DateTime? PaymentDate { get; private set; }

        public PaymentLineItemStatusEnum PaymentLineItemStatus { get; private set; }

        public bool IsRejected { get; private set; }
        public DateTime? RejectedDate { get; private set; }
        public Guid? RejectedById { get; private set; }

        private PaymentLineItem() { }

        public static PaymentLineItem GeneratePaymentLineItem(
            string rawData,
            Clinician? clinician,
            string rawClinicianName,
            decimal paymentAmount,
            decimal adjustmentAmount,
            PaymentAdjustmentCodeEnum adjustmentCode,
            DateTime dateOfService,
            string patientId,
            string cptCode,
            string paymentId,
            string payer,
            EHRUser appliedBy,
            ImportBatch importBatch,
            int rowNumber,
            string fingerprint,
            DateTime appliedDate,
            DateTime? paymentDate)
        {
            if (appliedBy == null) throw new ArgumentNullException(nameof(appliedBy));
            if (importBatch == null) throw new ArgumentNullException(nameof(importBatch));
            if (string.IsNullOrWhiteSpace(rawData))
            {
                throw new ArgumentException("Raw data for the payment line item cannot be empty or null");
            }
            if (string.IsNullOrWhiteSpace(rawClinicianName))
            {
                throw new ArgumentException("Raw clinician name for the payment line item cannot be empty or null");
            }
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                throw new ArgumentException("Fingerprint for the payment line item cannot be empty or null");
            }

            var item = new PaymentLineItem
            {
                Id = Guid.NewGuid(),
                RawData = rawData,
                PaymentAmount = paymentAmount,
                AdjustmentAmount = adjustmentAmount,
                PaymentAdjustmentCode = adjustmentCode,
                DateOfService = dateOfService,
                PatientId = patientId,
                CPTCode = cptCode,
                PaymentId = paymentId,
                Payer = payer,
                AppliedBy = appliedBy,
                AppliedById = appliedBy.Id,
                ImportBatch = importBatch,
                ImportBatchId = importBatch.Id,
                Fingerprint = fingerprint,
                RowNumber = rowNumber,
                AppliedDate = appliedDate,
                PaymentDate = paymentDate,
                RawClinicianName = rawClinicianName
            };

            if (clinician == null)
            {
                item.ClinicianId = null;
                item.PaymentLineItemStatus = PaymentLineItemStatusEnum.UNRESOLVED_CLINICIAN;
            }
            else
            {
                item.ClinicianId = clinician.ID;
                item.PaymentLineItemStatus = PaymentLineItemStatusEnum.VALID;
            }

            return item;
        }

        public static PaymentLineItem GenerateManualPaymentLineItem(
            string rawData,
            Clinician? clinician,
            string rawClinicianName,
            decimal paymentAmount,
            decimal adjustmentAmount,
            PaymentAdjustmentCodeEnum adjustmentCode,
            DateTime dateOfService,
            string patientId,
            string cptCode,
            string paymentId,
            string payer,
            EHRUser appliedBy,
            string fingerprint,
            DateTime appliedDate,
            DateTime? paymentDate)
        {
            if (appliedBy == null) throw new ArgumentNullException(nameof(appliedBy));
            if (string.IsNullOrWhiteSpace(rawData))
                throw new ArgumentException("Raw data cannot be empty or null");
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new ArgumentException("Fingerprint cannot be empty or null");

            var item = new PaymentLineItem
            {
                Id = Guid.NewGuid(),
                RawData = rawData,
                PaymentAmount = paymentAmount,
                AdjustmentAmount = adjustmentAmount,
                PaymentAdjustmentCode = adjustmentCode,
                DateOfService = dateOfService,
                PatientId = patientId,
                CPTCode = cptCode,
                PaymentId = paymentId,
                Payer = payer,
                AppliedBy = appliedBy,
                AppliedById = appliedBy.Id,
                ImportBatch = null,
                ImportBatchId = null,
                Fingerprint = fingerprint,
                RowNumber = 0,
                AppliedDate = appliedDate,
                PaymentDate = paymentDate,
                RawClinicianName = rawClinicianName ?? string.Empty
            };

            if (clinician == null)
            {
                item.ClinicianId = null;
                item.PaymentLineItemStatus = PaymentLineItemStatusEnum.UNRESOLVED_CLINICIAN;
            }
            else
            {
                item.ClinicianId = clinician.ID;
                item.PaymentLineItemStatus = adjustmentCode == PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK
                    ? PaymentLineItemStatusEnum.VALID
                    : PaymentLineItemStatusEnum.VALID;
            }

            return item;
        }

        public Code500Application ApplyCode500(decimal amount, Guid appliedByUserId, PayRun payRun)
        {
            if (PaymentAdjustmentCode != PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK)
                throw new InvalidOperationException("Cannot apply a non-code-500 payment.");
            if (ClinicianId == null)
                throw new InvalidOperationException("Cannot apply a code 500 payment with no resolved clinician.");
            if (amount <= 0)
                throw new ArgumentException("Amount to apply must be greater than 0.");
            if (amount > RemainingCode500Amount)
                throw new ArgumentException("Cannot apply more than the remaining outstanding balance.");

            Code500Application application = Code500Application.Create(this, amount, appliedByUserId, payRun);
            Code500Applications.Add(application);
            Code500AppliedAmount += amount;
            if (RemainingCode500Amount == 0)
                IsCode500Applied = true;
            PaymentLineItemStatus = PaymentLineItemStatusEnum.VALID;
            return application;
        }

        public void Reject(Guid rejectedByUserId)
        {
            if (PaymentAdjustmentCode != PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK)
                throw new InvalidOperationException("Cannot reject a non-code-500 payment.");
            if (IsRejected)
                throw new InvalidOperationException("Payment has already been rejected.");
            if (Code500AppliedAmount > 0)
                throw new InvalidOperationException("Cannot reject a payment that already has amounts applied to a pay run.");

            IsRejected = true;
            RejectedDate = DateTime.UtcNow;
            RejectedById = rejectedByUserId;
        }

        public void UpdateClinician(Clinician clinician)
        {
            if (clinician == null)
            {
                throw new ArgumentNullException("Clinician is null");
            }
            ClinicianId = clinician.ID;
            PaymentLineItemStatus = PaymentLineItemStatusEnum.VALID;
        }
    }
}
