using CMI.Contract.Common;

namespace CMI.Manager.ExternalContent
{
    internal interface IExternalContentManager
    {
        /// <summary>
        ///     Gets the digitization order data for a specific archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        DigitizationOrderDataResult GetDigitizationOrderData(string archiveRecordId);
    }
}