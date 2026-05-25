namespace PayrollBackendProject.Domain.Enums
{
    public enum PaymentAdjustmentCodeEnum
    {
        PATIENT_PAYMENT = 100,
        INSURANCE_PAYMENT = 102,
        INSURANCE_ADJUSTMENT = 200,
        PROBONO_WRITEOFF = 201,
        CONTRACT_WRITEOFF = 202,
        COPAY_OVERPAY_DEBIT = 203,
        QB_CREDIT_IN = 204,
        NOT_SEEN_CREDIT = 205,
        BAD_DEBT = 206,
        QB_CREDIT_XFER = 207,
        QB_CREDIT_REDUC = 208,
        DUP_VISIT_CREDIT = 209,
        INS_CREDIT = 210,
        INS_INTEREST = 211,
        ADJUSTMENT_DEBIT = 300,
        NSF_PAYMENT = 301,
        NSF_FEES = 302,
        COPAY_OVERPAY_CREDIT = 303,
        NSF_CHECK_REVERSAL = 304,
        NEGOTIATED_FEE = 400,
        APPEASEMENT_WRITEOFF = 401,
        INSURANCE_TAKEBACK = 500,
        PREV_PAID_INS = 501,
        PREV_CONTRACT_WRITEOFF = 503,
        PREV_CLIENT_PAYMENT = 504,
        ICC_PMT = 998,
        INS_REDUCTION = 999,
    }
}
