namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a payment line item returned by the API.
    /// </summary>
    /// <remarks>
    /// This DTO contains financial and reference data for an individual payment entry,
    /// typically derived from imported billing or EHR data.
    /// </remarks>
    public class PaymentLineItemDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the payment line item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the primary payment amount for the line item.
        /// </summary>
        public decimal PaymentAmount { get; set; }

        /// <summary>
        /// Gets or sets any adjustment amount applied to the payment.
        /// </summary>
        public decimal AdjustmentAmount { get; set; }

        /// <summary>
        /// Gets or sets the CPT (Current Procedural Terminology) code associated with the service.
        /// </summary>
        /// <remarks>
        /// This may be null if the code was not provided in the source data.
        /// </remarks>
        public string? CPTCode { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the patient associated with this payment.
        /// </summary>
        /// <remarks>
        /// This may be null if patient information is unavailable or unresolved.
        /// </remarks>
        public string? PatientId { get; set; }
    }
}