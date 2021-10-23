using System;

namespace NextBus.Presentation.Buses.Models.Result
{
    public class GetBusQueryResult
    {
        public Guid Id { get; set; }
        public int BusNo { get; set; }
        public string DriverName { get; set; }
        public string TakeOffPoint { get; set; }
        public string Destination { get; set; }
        public int Seat { get; set; }
        public int TotalAvailableSeat { get; set; }
        public bool IsVacant { get; set; }
    }
}
