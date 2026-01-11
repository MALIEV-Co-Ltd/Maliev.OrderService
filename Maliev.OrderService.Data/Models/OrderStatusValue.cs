namespace Maliev.OrderService.Data.Models
{
    /// <summary>
    /// Represents the 16 states of an order workflow.
    /// </summary>
    public enum OrderStatusValue
    {
        /// <summary>New order created</summary>
        New,
        /// <summary>Under technical review</summary>
        Reviewing,
        /// <summary>Rejected by engineering</summary>
        Rejected,
        /// <summary>Review complete, ready for quoting</summary>
        Reviewed,
        /// <summary>Quote issued to customer</summary>
        Quoted,
        /// <summary>Quote declined by customer</summary>
        Declined,
        /// <summary>Quote accepted, awaiting payment/PO</summary>
        Accepted,
        /// <summary>Quote expired</summary>
        Expired,
        /// <summary>Payment received</summary>
        Paid,
        /// <summary>Purchase order issued (B2B)</summary>
        POIssued,
        /// <summary>In production</summary>
        InProgress,
        /// <summary>Paused/Waiting</summary>
        OnHold,
        /// <summary>Production finished</summary>
        Finished,
        /// <summary>Shipped to customer</summary>
        Shipped,
        /// <summary>Reopened for rework</summary>
        Reopen,
        /// <summary>Cancelled</summary>
        Cancelled
    }
}
