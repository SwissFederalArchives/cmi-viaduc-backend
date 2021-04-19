using CMI.Contract.Harvest;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbResyncAccess
    {
        /// <summary>
        ///     Initiates a full resync of all archive records.
        /// </summary>
        /// <param name="info">Information about who and when the request was sent.</param>
        /// <returns>Number of added records to the mutation table</returns>
        public int InitiateFullResync(ResyncRequestInfo info)
        {
            var affectedRecords = dataProvider.InitiateFullResync();
            return affectedRecords;
        }
    }
}