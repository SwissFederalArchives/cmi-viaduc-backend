namespace CMI.Contract.Common
{
    /// <summary>
    ///     Class with information about a status update
    /// </summary>
    public class MutationStatusInfo
    {
        /// <summary>
        ///     Thr primary id of the mutation to update.
        /// </summary>
        public long MutationId { get; set; }

        /// <summary>
        /// The archive record id that is affected
        /// </summary>
        public string ArchiveRecordId { get; set; }

        /// <summary>
        /// The type of mutation "Update" or "Delete"
        /// </summary>
        public string MutationType { get; set; }

        /// <summary>
        ///     The new status to set
        /// </summary>
        public ActionStatus NewStatus { get; set; }

        /// <summary>
        ///     Optionally a indication from which status the update must take place
        /// </summary>
        public ActionStatus? ChangeFromStatus { get; set; }

        /// <summary>
        ///     The error message if the synchronization was not successful
        /// </summary>
        public string ErrorMessage { get; set; }


        /// <summary>
        ///     The stack trace if the synchronization was not successful.
        /// </summary>
        public string StackTrace { get; set; }
    }
}