namespace CMI.Contract.Common
{
    /// <summary>
    ///     The result of a request to the external data manager asking for the data required for a
    ///     digitization order.
    /// </summary>
    public class DigitizationOrderDataResult
    {
        public DigitalisierungsAuftrag DigitizationOrder { get; set; }

        public bool Success { get; set; }

        public string ErrorMessage { get; set; }
    }
}