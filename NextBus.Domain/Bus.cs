namespace NextBus.Domain
{
    public class Bus : BaseEntity
    {
        public string DriverAppUserId { get; set; }
        public Driver Driver { get; set; }
        public string TakeOffPoint { get; set; }
        public string Destination { get; set; }
        public int Seat { get; set; }
        public int TotalAvailableSeat { get; set; }
        public bool IsVacant { get; set; }
    }
}