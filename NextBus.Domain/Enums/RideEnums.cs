namespace NextBus.Domain.Enums
{
    public enum RideStatus
    {
        Scheduled = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum BookingStatus
    {
        Requested = 1,
        Accepted = 2,
        Rejected = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Refunded = 3
    }

    public enum TransactionType
    {
        TopUp = 1,
        RideFare = 2,
        RideEarnings = 3,
        Refund = 4,
        Withdrawal = 5
    }
}
