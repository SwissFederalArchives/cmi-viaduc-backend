using CMI.Contract.Common;

namespace CMI.Contract.Harvest
{
    public interface IDbExternalContentAccess
    {
        /// <summary>
        ///     Gets the digitization order data for a given archive record.
        ///     If the archive record cannot be found, success is returned, but the
        ///     but the contained DigitizationOrder property will have most
        ///     of its properties set to "keine Angabe".
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        DigitizationOrderDataResult GetDigitizationOrderData(string archiveRecordId);

        /// <summary>
        ///  Gets the SyncInfoForReportResult from the AIS.
        /// Need for create Report
        /// </summary>
        /// <returns>A Data type that have records with the mutationsIds.</returns>
        SyncInfoForReportResult GetReportExternalContent(int[] mutationsIds);
    }
}
