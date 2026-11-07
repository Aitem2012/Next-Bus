using System;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class RideBooking : BaseEntity
    {
        public Guid RideId { get; set; }
        public string PassengerId { get; set; } = string.Empty;

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;

        public int SeatsBooked { get; set; } = 1;
        public decimal PricePerSeat { get; set; }
        public decimal TotalFare { get; set; }

        public BookingStatus BookingStatus { get; set; } = BookingStatus.Requested;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public string? Notes { get; set; }

        public virtual Ride? Ride { get; set; }
        public virtual AppUser? Passenger { get; set; }
    }
}
