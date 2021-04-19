namespace CMI.Contract.Messaging
{
    public interface IResyncArchiveDatabaseStarted
    {
        /// <summary>
        ///     Gets the number of inserted records into the mutation table.
        /// </summary>
        int InsertedRecords { get; set; }
    }
}