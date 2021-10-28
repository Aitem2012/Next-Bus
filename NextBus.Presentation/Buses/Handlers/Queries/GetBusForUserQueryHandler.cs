using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Buses.Models.Result;
using NextBus.Presentation.Buses.Queries;

namespace NextBus.Presentation.Buses.Handlers.Queries
{
    public class GetBusForUserQueryHandler : IRequestHandler<GetBusForUserQuery, IEnumerable<GetBusQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        public GetBusForUserQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<GetBusQueryResult>> Handle(GetBusForUserQuery request, CancellationToken cancellationToken)
        {
            var buses = await _context.Buses.Where(x => x.Destination.ToLower() == request.QueryParams.ToLower() || x.TakeOffPoint.ToLower() == request.QueryParams.ToLower() && x.TotalAvailableSeat >0)
                .ToListAsync(cancellationToken);
            return _mapper.Map<IEnumerable<GetBusQueryResult>>(buses);
        }
    }
}
