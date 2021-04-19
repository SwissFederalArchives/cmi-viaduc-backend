using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Manager.ExternalContent
{
    public class ExternalContentManager : IExternalContentManager
    {
        private readonly IDbDigitizationOrderAccess dbDigitizationOrderAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExternalContentManager" /> class.
        /// </summary>
        /// <param name="dbDigitizationOrderAccess">The database digitization order access.</param>
        public ExternalContentManager(IDbDigitizationOrderAccess dbDigitizationOrderAccess)
        {
            this.dbDigitizationOrderAccess = dbDigitizationOrderAccess;
        }

        /// <summary>
        ///     Gets the digitization order data for a specific archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        public DigitizationOrderDataResult GetDigitizationOrderData(string archiveRecordId)
        {
            return dbDigitizationOrderAccess.GetDigitizationOrderData(archiveRecordId);
        }
    }
}