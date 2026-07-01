using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a pay statement for a clinician within a specific pay run.
    /// </summary>
    /// <remarks>
    /// This DTO aggregates payment line items and calculated totals for a clinician,
    /// including cost share adjustments and overall payment summaries.
    /// </remarks>
    public class PayStatementDTO
    {
        /// <summary>
        /// Gets the unique identifier for the pay statement.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the clinician associated with this pay statement.
        /// </summary>
        public ClinicianResponseDTO Clinician { get; private set; }

        /// <summary>
        /// Gets the list of payment line items included in this pay statement.
        /// </summary>
        public List<PaymentLineItemDTO> LineItems { get; private set; }

        /// <summary>
        /// Gets the identifier of the pay run associated with this statement.
        /// </summary>
        public Guid PayRun { get; private set; }

        /// <summary>
        /// Gets the total payment amount before adjustments.
        /// </summary>
        public decimal TotalPayment { get; private set; }

        /// <summary>
        /// Gets the total payment amount after applying cost share adjustments.
        /// </summary>
        public decimal CostShareAdjustedPayment { get; private set; }

        /// <summary>
        /// Gets the total adjustment amount applied to the payment.
        /// </summary>
        public decimal TotalAdjustment { get; private set; }

        /// <summary>
        /// Gets the clinician cost share percentage snapshotted at the time the statement was generated.
        /// </summary>
        public decimal CostShareSnapshot { get; private set; }

        /// <summary>
        /// Gets the approval state of this pay statement.
        /// </summary>
        public int ApprovalState { get; private set; }

        /// <summary>
        /// Gets the ID of the user who approved or rejected this statement, if any.
        /// </summary>
        public Guid? ApprovedRejectedBy { get; private set; }

        /// <summary>
        /// Gets the timestamp when this statement was approved or rejected, if any.
        /// </summary>
        public DateTime? ApprovedRejectedOn { get; private set; }

        public PayStatementDTO(
            Guid id,
            ClinicianResponseDTO clinician,
            List<PaymentLineItemDTO> lineItems,
            Guid payRun,
            decimal totalPayment,
            decimal costShareAdjustedPayment,
            decimal totalAdjustment,
            decimal costShareSnapshot,
            int approvalState,
            Guid? approvedRejectedBy,
            DateTime? approvedRejectedOn)
        {
            Id = id;
            Clinician = clinician;
            LineItems = lineItems;
            PayRun = payRun;
            TotalPayment = totalPayment;
            CostShareAdjustedPayment = costShareAdjustedPayment;
            TotalAdjustment = totalAdjustment;
            CostShareSnapshot = costShareSnapshot;
            ApprovalState = approvalState;
            ApprovedRejectedBy = approvedRejectedBy;
            ApprovedRejectedOn = approvedRejectedOn;
        }
    }
}