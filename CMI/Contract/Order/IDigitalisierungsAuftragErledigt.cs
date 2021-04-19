using System;

namespace CMI.Contract.Order
{
    public interface IDigitalisierungsAuftragErledigt
    {
        string ArchiveRecordId { get; set; }

        int OrderItemId { get; set; }

        DateTime OrderDate { get; set; }

        /// <summary>
        ///     Gets or sets the order user identifier of the user that requested the digitization.
        /// </summary>
        string OrderUserId { get; set; }

        string OrderUserRolePublicClient { get; set; }
    }

    public class DigitalisierungsAuftragErledigt : IDigitalisierungsAuftragErledigt
    {
        public string ArchiveRecordId { get; set; }
        public int OrderItemId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderUserId { get; set; }
        public string OrderUserRolePublicClient { get; set; }
    }
}