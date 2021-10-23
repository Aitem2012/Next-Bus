using MediatR;
using NextBus.Presentation.Buses.Models.Result;

namespace NextBus.Presentation.Buses.Commands
{
    public class CreateBusCommand: IRequest<GetBusQueryResult>
    {
        public int BusNo { get; set; }
        public string TakeOffPoint { get; set; }
        public string Destination { get; set; }
        public int Seat { get; set; }
        public int TotalAvailableSeat { get; set; }
        public bool IsVacant { get; set; }
    }
}
