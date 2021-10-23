using MediatR;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Users.Commands;

namespace NextBus.Presentation.Drivers.Commands
{
    public class UpdateDriverCommand : IRequest<GetDriverQueryResult>
    {
        public string Id { get; set; }
        public UpdateBusCommand Bus { get; set; }
        public UpdateUserCommand User { get; set; }
    }
}
