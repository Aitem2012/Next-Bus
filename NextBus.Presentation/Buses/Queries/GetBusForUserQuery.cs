using System.Collections.Generic;
using MediatR;
using NextBus.Presentation.Buses.Models.Result;

namespace NextBus.Presentation.Buses.Queries
{
    public class GetBusForUserQuery : IRequest<IEnumerable<GetBusQueryResult>>
    {
        public string QueryParams { get; set; }
    }
}
