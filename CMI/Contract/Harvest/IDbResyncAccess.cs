namespace CMI.Contract.Harvest
{
    public interface IDbResyncAccess
    {
        /// <summary>
        ///     Initiates a full resync of all archive records.
        /// </summary>
        /// <param name="info">Information about who and when the request was sent.</param>
        /// <returns>Number of added records to the mutation table</returns>
        int InitiateFullResync(ResyncRequestInfo info);
    }
}