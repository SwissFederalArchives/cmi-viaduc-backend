using CMI.Contract.Common;
using System.Threading.Tasks;

namespace CMI.Manager.ExternalContent
{
    public interface IExternalContentManager
    {
        /// <summary>
        ///     Gets the digitization order data for a specific archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        Task<DigitizationOrderDataResult> GetDigitizationOrderData(string archiveRecordId);
    }
}