using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.Mappings
{
    public static class PaymentLineItemMapper
    {
        public static PaymentLineItem DtoToDomain(PaymentCsvRow row, string rawData, Clinician? clinician, EHRUser appliedBy, ImportBatch importBatch, int rowNumber, string fingerprint)
        {
            // TODO MAKE THIS MORE ROBUST - WHEN CODE ISN'T FOUND SHOULDN'T CRASH, SHOULD REPORT ERROR
            PaymentAdjustmentCodeEnum codeEnum;
            var acctNo = row.AcctNo?.Trim();
            if (int.TryParse(acctNo, out int code) && Enum.IsDefined(typeof(PaymentAdjustmentCodeEnum), code))
            {
                codeEnum = (PaymentAdjustmentCodeEnum)code;
            }
            else
            {
                throw new ArgumentException($"Invalid adjustment code: {row.AcctNo}");
            }

            return PaymentLineItem.GeneratePaymentLineItem(
                rawData,
                clinician,
                row.ClinicianName,
                -(row.AppliedPayments ?? 0),
                -(row.AppliedAdjustment ?? 0),
                codeEnum,
                NormalizeToUTCRequired(row.DOS),
                row.PatientID,
                row.CPT,
                row.PaymentID ?? string.Empty,
                row.Payer ?? string.Empty,
                appliedBy,
                importBatch,
                rowNumber,
                fingerprint,
                NormalizeToUTCRequired(row.AppliedDate),
                NormalizeToUTCOptional(row.PaymentDate)
                );
        }

        public static UnappliedCode500ResponseDTO DomainToUnappliedCode500DTO(PaymentLineItem item)
        {
            return new UnappliedCode500ResponseDTO
            {
                Id = item.Id,
                PaymentAmount = item.PaymentAmount,
                AdjustmentAmount = item.AdjustmentAmount,
                DateOfService = item.DateOfService,
                PatientId = item.PatientId,
                CPTCode = item.CPTCode,
                PaymentId = item.PaymentId,
                Payer = item.Payer,
                RawClinicianName = item.RawClinicianName,
                AppliedDate = item.AppliedDate,
                PaymentDate = item.PaymentDate,
                ImportBatchId = item.ImportBatchId,
                AppliedAmount = item.Code500AppliedAmount,
                RemainingAmount = item.RemainingCode500Amount
            };
        }

        public static PaymentLineItem ManualDtoToDomain(ManualPaymentRequestDTO dto, Clinician? clinician, EHRUser appliedBy, string fingerprint)
        {
            if (!Enum.IsDefined(typeof(PaymentAdjustmentCodeEnum), dto.PaymentAdjustmentCode))
                throw new ArgumentException($"Invalid adjustment code: {dto.PaymentAdjustmentCode}");

            var codeEnum = (PaymentAdjustmentCodeEnum)dto.PaymentAdjustmentCode;
            string rawData = $"MANUAL|{dto.PatientId}|{dto.CPTCode}|{dto.PaymentId ?? string.Empty}|{dto.DateOfService:O}";

            return PaymentLineItem.GenerateManualPaymentLineItem(
                rawData,
                clinician,
                dto.RawClinicianName,
                dto.PaymentAmount ?? 0m,
                dto.AdjustmentAmount ?? 0m,
                codeEnum,
                NormalizeToUTCRequired(dto.DateOfService),
                dto.PatientId,
                dto.CPTCode,
                dto.PaymentId ?? string.Empty,
                dto.Payer ?? string.Empty,
                appliedBy,
                fingerprint,
                NormalizeToUTCRequired(dto.AppliedDate),
                NormalizeToUTCOptional(dto.PaymentDate)
            );
        }

        private static DateTime? NormalizeToUTCOptional(DateTime? dt)
        {
            if (dt == null)
            {
                return null;
            }
            return dt.Value.Kind switch
            {
                DateTimeKind.Utc => dt.Value,
                DateTimeKind.Local => dt.Value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc),
                _ => dt.Value
            };
        }

        private static DateTime NormalizeToUTCRequired(DateTime? dt)
        {
            if (dt == null)
            {
                throw new Exception("Parsed a row with missing or invalid date.");
            }
            return dt.Value.Kind switch
            {
                DateTimeKind.Utc => dt.Value,
                DateTimeKind.Local => dt.Value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc),
                _ => dt.Value
            };
        }

        public static PaymentLineItemDTO DomainToDto(PaymentLineItem domainLineItem)
        {
            return new PaymentLineItemDTO
            {
                Id = domainLineItem.Id,
                PaymentAmount = domainLineItem.PaymentAmount,
                AdjustmentAmount = domainLineItem.AdjustmentAmount,
                CPTCode = domainLineItem.CPTCode,
                PatientId = domainLineItem.PatientId,
                RawClinicianName = domainLineItem.RawClinicianName,
                DateOfService = domainLineItem.DateOfService
            };
        }
    }
}
