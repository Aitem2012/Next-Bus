using System;
using System.Collections.Generic;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class Vehicle : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.PrivateCar;
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual AppUser? AppUser { get; set; }
        public virtual ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }
}
