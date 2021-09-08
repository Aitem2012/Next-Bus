using System;

namespace NextBus.Domain
{
    public class Driver
    {
        public string DriverIdentificationNumber { get; set; }
        
        public string AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }
    }
}
