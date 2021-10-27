using NextBus.Domain.BaseEntities;
using NextBus.Domain.Drivers;
using System;

namespace NextBus.Domain.Buses
{
    public class Bus : BaseEntity
    {
        public int BusNo { get; set; }
        public string TakeOffPoint { get; set; }
        public string Destination { get; set; }
        public int Seat { get; set; }
        public int TotalAvailableSeat { get; set; }
        public bool IsVacant { get; set; }
        //public virtual Driver Driver { get; set; }
        //public string DriverId { get; set; }
    }
}