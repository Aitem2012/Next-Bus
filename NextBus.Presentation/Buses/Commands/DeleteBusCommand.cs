using System;
using MediatR;
using NextBus.Presentation.Common.Models.Results;

namespace NextBus.Presentation.Buses.Commands
{
    public class DeleteBusCommand : IRequest<DeleteCommandResult>
    {
        public Guid Id { get; set; }
    }
}
