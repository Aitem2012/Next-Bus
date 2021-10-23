using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Drivers.Queries;

namespace NextBus.Presentation.Drivers.Handlers.Queries
{
    public class GetDriverQueryHandler : IRequestHandler<GetDriversQuery, IEnumerable<GetDriverQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetDriverQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<GetDriverQueryResult>> Handle(GetDriversQuery request, CancellationToken cancellationToken)
        {
            var drivers = await _context.Drivers.Include(a => a.AppUser)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<GetDriverQueryResult>>(drivers);
        }
    }
}
