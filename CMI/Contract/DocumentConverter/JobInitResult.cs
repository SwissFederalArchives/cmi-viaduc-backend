namespace CMI.Contract.DocumentConverter
{
    /// <summary>
    ///     Class JobResult.
    /// </summary>
    public class JobInitResult : IErrorResult
    {
        /// <summary>
        ///     The username for the sftp server
        /// </summary>
        public string User { get; set; }

        /// <summary>
        ///     The password to use for uploading with sftp
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     The URL to upload to
        /// </summary>
        public string UploadUrl { get; set; }

        /// <summary>
        ///     The port to use for uploading with sftp
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>
        ///     Gets the unique job identifier.
        /// </summary>
        public string JobGuid { get; set; }

        /// <summary>
        ///     If the job could not be initiated or started the job is invalid
        /// </summary>
        public bool IsInvalid { get; set; }

        /// <summary>
        ///     Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}