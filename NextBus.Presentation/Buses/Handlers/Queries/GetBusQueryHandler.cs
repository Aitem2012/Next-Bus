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
    public class GetBusQueryHandler : IRequestHandler<GetBusQuery, GetBusQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetBusQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<GetBusQueryResult> Handle(GetBusQuery request, CancellationToken cancellationToken)
        {
            var bus = await _context.Buses.SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken);

            return _mapper.Map(bus, new GetBusQueryResult());
        }
    }
}
