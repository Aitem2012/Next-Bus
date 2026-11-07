using System;
using NextBus.Domain.Enums;

namespace NextBus.Application.DTOs
{
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateVehicleDto
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.PrivateCar;
        public int TotalSeats { get; set; }
    }
}
