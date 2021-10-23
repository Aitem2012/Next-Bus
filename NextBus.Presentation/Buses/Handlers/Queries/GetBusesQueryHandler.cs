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
    public class GetBusesQueryHandler : IRequestHandler<GetBusesQuery, IEnumerable<GetBusQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetBusesQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<GetBusQueryResult>> Handle(GetBusesQuery request, CancellationToken cancellationToken)
        {
            var buses = await _context.Buses.Select(x => x).ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<GetBusQueryResult>>(buses);
        }
    }
}
