using System.Collections.Generic;
using MediatR;
using NextBus.Presentation.Drivers.Models.Results;

namespace NextBus.Presentation.Drivers.Queries
{
    public class GetDriversQuery : IRequest<IEnumerable<GetDriverQueryResult>>
    {
        
    }
}
