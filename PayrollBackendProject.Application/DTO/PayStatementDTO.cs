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
        /// Initializes a new instance of the <see cref="PayStatementDTO"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the pay statement.</param>
        /// <param name="clinician">The clinician associated with the statement.</param>
        /// <param name="lineItems">The list of payment line items.</param>
        /// <param name="payRun">The identifier of the associated pay run.</param>
        /// <param name="totalPayment">The total payment before adjustments.</param>
        /// <param name="costShareAdjustedPayment">The total payment after cost share adjustments.</param>
        /// <param name="totalAdjustment">The total adjustment applied.</param>
        public PayStatementDTO(
            Guid id,
            ClinicianResponseDTO clinician,
            List<PaymentLineItemDTO> lineItems,
            Guid payRun,
            decimal totalPayment,
            decimal costShareAdjustedPayment,
            decimal totalAdjustment)
        {
            Id = id;
            Clinician = clinician;
            LineItems = lineItems;
            PayRun = payRun;
            TotalPayment = totalPayment;
            CostShareAdjustedPayment = costShareAdjustedPayment;
            TotalAdjustment = totalAdjustment;
        }
    }
}