using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a pay run returned by the API, including summary totals and status information.
    /// </summary>
    /// <remarks>
    /// A pay run defines a time period over which payments are aggregated and processed into pay statements.
    /// This DTO includes financial totals as well as generation and approval state.
    /// </remarks>
    public class PayRunResponseDTO
    {
        /// <summary>
        /// Gets the unique identifier for the pay run.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the start date of the pay run period.
        /// </summary>
        public DateTime StartDate { get; private set; }

        /// <summary>
        /// Gets the end date of the pay run period.
        /// </summary>
        public DateTime EndDate { get; private set; }

        /// <summary>
        /// Gets the total applied payment amount before adjudication.
        /// </summary>
        public decimal TotalApplied { get; private set; }

        /// <summary>
        /// Gets the total adjudicated payment amount after processing adjustments.
        /// </summary>
        public decimal TotalAdjudicated { get; private set; }

        /// <summary>
        /// Gets the sum of all pay statement totals associated with this pay run.
        /// </summary>
        public decimal StatementTotals { get; private set; }

        /// <summary>
        /// Gets the current generation status of the pay run.
        /// </summary>
        /// <remarks>
        /// Indicates whether the pay run is pending, in progress, or completed.
        /// </remarks>
        public PayRunStatusEnum GenerationStatus { get; private set; }

        /// <summary>
        /// Gets the approval status of the pay run.
        /// </summary>
        /// <remarks>
        /// Indicates whether the pay run is pending approval, approved, or rejected.
        /// </remarks>
        public ApprovalStateEnum ApprovalStatus { get; private set; }

        /// <summary>
        /// Gets the identifier of the user who approved or rejected the pay run.
        /// </summary>
        /// <remarks>
        /// This value is null if the pay run has not yet been reviewed.
        /// </remarks>
        public Guid? ApprovedRejectedBy { get; private set; }

        /// <summary>
        /// Gets the timestamp when the pay run was approved or rejected.
        /// </summary>
        /// <remarks>
        /// This value is null if the pay run has not yet been reviewed.
        /// </remarks>
        public DateTime? ApprovedRejectedOn { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PayRunResponseDTO"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the pay run.</param>
        /// <param name="startDate">The start date of the pay run period.</param>
        /// <param name="endDate">The end date of the pay run period.</param>
        /// <param name="totalApplied">The total applied payment amount.</param>
        /// <param name="totalAdjudicated">The total adjudicated payment amount.</param>
        /// <param name="statementTotals">The total of all associated pay statements.</param>
        /// <param name="generationStatus">The current generation status of the pay run.</param>
        /// <param name="approvalStatus">The approval status of the pay run.</param>
        /// <param name="approvedRejectedBy">The user who approved or rejected the pay run.</param>
        /// <param name="approvedRejectedOn">The timestamp of approval or rejection.</param>
        public PayRunResponseDTO(
            Guid id,
            DateTime startDate,
            DateTime endDate,
            decimal totalApplied,
            decimal totalAdjudicated,
            decimal statementTotals,
            PayRunStatusEnum generationStatus,
            ApprovalStateEnum approvalStatus,
            Guid? approvedRejectedBy,
            DateTime? approvedRejectedOn)
        {
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            TotalApplied = totalApplied;
            TotalAdjudicated = totalAdjudicated;
            StatementTotals = statementTotals;
            GenerationStatus = generationStatus;
            ApprovalStatus = approvalStatus;
            ApprovedRejectedBy = approvedRejectedBy;
            ApprovedRejectedOn = approvedRejectedOn;
        }
    }
}