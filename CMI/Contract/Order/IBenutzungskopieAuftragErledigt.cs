namespace CMI.Contract.Order
{
    public interface IBenutzungskopieAuftragErledigt
    {
        string ArchiveRecordId { get; set; }

        int OrderItemId { get; set; }

        ApproveStatus OrderApproveStatus { get; set; }

        /// <summary>
        ///     Gets or sets the order user identifier of the user that requested the digitization.
        /// </summary>
        string OrderUserId { get; set; }
    }
}