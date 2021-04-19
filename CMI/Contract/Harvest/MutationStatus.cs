using CMI.Contract.Common;

namespace CMI.Contract.Harvest
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