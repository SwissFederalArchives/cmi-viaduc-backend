namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordRemoved
    {
        int MutationId { get; set; }
        bool ActionSuccessful { get; set; }
        string ErrorMessage { get; set; }
        string StackTrace { get; set; }
    }
}