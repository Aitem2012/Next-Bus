using System;
using System.Collections.Generic;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class Ride : BaseEntity
    {
        public string DriverId { get; set; } = string.Empty;
        public Guid VehicleId { get; set; }

        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        // Maximum of two intermediate stops
        public string? Stop1 { get; set; }
        public string? Stop2 { get; set; }

        public DateTime DepartureTime { get; set; }
        public int TotalSeatsOffered { get; set; }
        public int AvailableSeats { get; set; }
        public decimal PricePerSeat { get; set; }

        public RideStatus Status { get; set; } = RideStatus.Scheduled;
        public bool IsPublicTransit { get; set; } = false;
        public string? Notes { get; set; }

        public virtual AppUser? Driver { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
        public virtual ICollection<RideBooking> Bookings { get; set; } = new List<RideBooking>();
    }
}
