namespace CMI.Contract.DocumentConverter
{
    public interface IErrorResult
    {
        /// <summary>
        ///     If the job could not be initiated or started the job is invalid
        /// </summary>
        bool IsInvalid { get; set; }

        /// <summary>
        ///     Gets or sets the error message.
        /// </summary>
        string ErrorMessage { get; set; }
    }
}