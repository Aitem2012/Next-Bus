using System;
using NextBus.Domain.Buses;
using NextBus.Domain.Users;

namespace NextBus.Domain.Drivers
{
    public class Driver
    {
        public string DriverIdentificationNumber { get; set; }
        public string AppUserId { get; set; }
        public Guid BusId { get; set; }
        public virtual Bus Bus { get; set; }
        public virtual AppUser AppUser { get; set; }
        
    }
}
