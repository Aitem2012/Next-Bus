using MediatR;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Core;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Users.Commands;

namespace NextBus.Presentation.Drivers.Commands
{
    public class CreateDriverCommand : IRequest<Result<GetDriverQueryResult>>
    {
        public CreateUserCommand User { get; set; }
        public CreateBusCommand Bus { get; set; }
        public string DriverIdentificationNumber { get; set; }
    }
}
