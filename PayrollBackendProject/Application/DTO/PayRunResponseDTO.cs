using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.DTO
{
    public class PayRunResponseDTO
    {
        public Guid Id { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public Decimal TotalApplied { get; private set; }
        public Decimal TotalAdjudicated { get; private set; }
        public Decimal StatementTotals { get; private set; }
        public PayRunStatusEnum GenerationStatus { get; private set; }
        public String ApprovalStatus { get; private set; }
        public Guid? ApprovedRejectedBy { get; private set; }
        public DateTime? ApprovedRejectedOn { get; private set; }


        public PayRunResponseDTO (Guid id, DateTime startDate, DateTime endDate, decimal totalApplied, decimal totalAdjudicated, decimal statementTotals, PayRunStatusEnum generationStatus, ApprovalStateEnum approvalState, Guid? approvedRejectedBy, DateTime? approvedRejectedOn)
        {
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            TotalApplied = totalApplied;
            TotalAdjudicated = totalAdjudicated;
            StatementTotals = statementTotals;
            GenerationStatus = generationStatus;
            ApprovalStatus = approvalState.ToString();
            ApprovedRejectedBy = approvedRejectedBy;
            ApprovedRejectedOn = approvedRejectedOn;
        }
    }
}
