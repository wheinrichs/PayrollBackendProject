namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a request to apply a specific dollar amount of a 500-code (insurance takeback)
    /// line item's remaining balance to the pay run being generated.
    /// </summary>
    public class Code500ApplicationRequestDTO
    {
        public Guid PaymentLineItemId { get; set; }
        public decimal Amount { get; set; }
    }
}
