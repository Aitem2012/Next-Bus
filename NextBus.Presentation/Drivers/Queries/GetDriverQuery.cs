using MediatR;
using NextBus.Presentation.Drivers.Models.Results;

namespace NextBus.Presentation.Drivers.Queries
{
    public class GetDriverQuery : IRequest<GetDriverQueryResult>
    {
        public string Id { get; set; }
    }
}
