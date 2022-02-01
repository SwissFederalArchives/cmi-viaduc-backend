using System;
using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Manager.ExternalContent
{
    public class ExternalContentManager : IExternalContentManager, IReportExternalContentManager
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

        /// <summary>
        ///  Gets the SyncInfoForReportResult from the AIS.
        /// Need for create Report
        /// </summary>
        /// <returns>A Data type that have records with the mutationsIds.</returns>
        public SyncInfoForReportResult GetReportExternalContent(int[] mutationsIds)
        {
            try
            {
                return dbExternalContentAccess.GetReportExternalContent(mutationsIds);
            }
            catch (Exception e)
            {
                return new SyncInfoForReportResult { ErrorMessage = e.Message };
            }
        }
    }
}
