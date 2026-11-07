using System;
using NextBus.Domain.Enums;

namespace NextBus.Application.DTOs
{
    public class RideDto
    {
        public Guid Id { get; set; }
        public string DriverId { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;

        public Guid VehicleId { get; set; }
        public string VehicleDescription { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }

        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? Stop1 { get; set; }
        public string? Stop2 { get; set; }

        public DateTime DepartureTime { get; set; }
        public int TotalSeatsOffered { get; set; }
        public int AvailableSeats { get; set; }
        public decimal PricePerSeat { get; set; }

        public RideStatus Status { get; set; }
        public bool IsPublicTransit { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateRideDto
    {
        public Guid VehicleId { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? Stop1 { get; set; }
        public string? Stop2 { get; set; }
        public DateTime DepartureTime { get; set; }
        public int AvailableSeats { get; set; }
        public decimal PricePerSeat { get; set; }
        public bool IsPublicTransit { get; set; }
        public string? Notes { get; set; }
    }

    public class RideSearchQueryDto
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public DateTime? Date { get; set; }
        public int? RequiredSeats { get; set; }
        public bool? IsPublicTransit { get; set; }
    }
}

