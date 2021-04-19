namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordUpdated
    {
        int MutationId { get; set; }
        bool ActionSuccessful { get; set; }
        string ErrorMessage { get; set; }
        string StackTrace { get; set; }
        int PrimaerdatenAuftragId { get; set; }
        int ArchiveRecordId { get; set; }
    }
}