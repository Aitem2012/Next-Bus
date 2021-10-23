﻿using MediatR;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Users.Commands;

namespace NextBus.Presentation.Drivers.Commands
{
    public class CreateDriverCommand : IRequest<GetDriverQueryResult>
    {
        public CreateUserCommand User { get; set; }
        public CreateBusCommand Bus { get; set; }
        public string DriverIdentificationNumber { get; set; }
    }
}
