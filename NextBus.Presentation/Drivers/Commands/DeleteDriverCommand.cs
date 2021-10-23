using MediatR;
using NextBus.Presentation.Common.Models.Results;

namespace NextBus.Presentation.Drivers.Commands
{
    public class DeleteDriverCommand : IRequest<DeleteCommandResult>
    {
        public string Id { get; set; }
    }
}
