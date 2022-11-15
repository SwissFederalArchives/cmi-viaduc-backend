using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Manager.ExternalContent
{
    public class ExternalContentManager : IExternalContentManager
    {
        private readonly IDbExternalContentAccess dbExternalContentAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExternalContentManager" /> class.
        /// </summary>
        /// <param name="dbExternalContentAccess">The database external Content.</param>
        public ExternalContentManager(IDbExternalContentAccess dbExternalContentAccess)
        {
            this.dbExternalContentAccess = dbExternalContentAccess;
        }

        /// <summary>
        ///     Gets the digitization order data for a specific archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        public DigitizationOrderDataResult GetDigitizationOrderData(string archiveRecordId)
        {
            return dbExternalContentAccess.GetDigitizationOrderData(archiveRecordId);
        }
    }
}
