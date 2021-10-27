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
    public class GetDriversQueryHandler : IRequestHandler<GetDriverQuery, GetDriverQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetDriversQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<GetDriverQueryResult> Handle(GetDriverQuery request, CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers.Include(e => e.AppUser).Include(e => e.Bus).SingleAsync(x => x.AppUserId == request.Id, cancellationToken);

            return _mapper.Map(driver, new GetDriverQueryResult());
        }
    }
}
