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
        public AdjustmentCodeEnum AdjustmentCode { get; private set; }

        public DateTime DateOfService { get; private set; }

        public string PatientId { get; private set; } = string.Empty;
        public string CPTCode { get; private set; } = string.Empty;
        public string PaymentId { get; private set; } = string.Empty;
        public string Payer { get; private set; } = string.Empty;

        public EHRUser AppliedBy { get; private set; } = null!;
        public Guid AppliedById { get; private set; }

        public ImportBatch ImportBatch { get; private set; } = null!;
        public Guid ImportBatchId { get; private set; }

        public string Fingerprint { get; private set; } = string.Empty;

        public int RowNumber { get; private set; }

        public DateTime AppliedDate { get; private set; }
        public DateTime? PaymentDate { get; private set; }

        public PaymentLineItemStatusEnum PaymentLineItemStatus { get; private set; }

        private PaymentLineItem() { }

        public static PaymentLineItem GeneratePaymentLineItem(
            string rawData,
            Clinician? clinician,
            string rawClinicianName,
            decimal paymentAmount,
            decimal adjustmentAmount,
            AdjustmentCodeEnum adjustmentCode,
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

            var item = new PaymentLineItem
            {
                Id = Guid.NewGuid(),
                RawData = rawData,
                PaymentAmount = paymentAmount,
                AdjustmentAmount = adjustmentAmount,
                AdjustmentCode = adjustmentCode,
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
