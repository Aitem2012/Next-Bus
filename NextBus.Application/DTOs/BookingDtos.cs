using System;
using NextBus.Domain.Enums;

namespace NextBus.Application.DTOs
{
    public class RideBookingDto
    {
        public Guid Id { get; set; }
        public Guid RideId { get; set; }
        public string PassengerId { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;

        public int SeatsBooked { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal TotalFare { get; set; }

        public BookingStatus BookingStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? Notes { get; set; }

        // Embedded summary of ride
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
    }

    public class CreateBookingDto
    {
        public Guid RideId { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public int SeatsBooked { get; set; } = 1;
        public string? Notes { get; set; }
    }

    public class RespondBookingDto
    {
        public bool Accept { get; set; }
        public string? Reason { get; set; }
    }
}
