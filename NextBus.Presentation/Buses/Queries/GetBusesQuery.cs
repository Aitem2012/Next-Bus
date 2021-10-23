using System.Collections.Generic;
using MediatR;
using NextBus.Presentation.Buses.Models.Result;

namespace NextBus.Presentation.Buses.Queries
{
    public class GetBusesQuery : IRequest<IEnumerable<GetBusQueryResult>>
    {
        
    }
}
