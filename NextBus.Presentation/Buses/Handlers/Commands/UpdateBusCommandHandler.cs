using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Buses.Models.Result;

namespace NextBus.Presentation.Buses.Handlers.Commands
{
    public class UpdateBusCommandHandler : IRequestHandler<UpdateBusCommand, GetBusQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public UpdateBusCommandHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<GetBusQueryResult> Handle(UpdateBusCommand request, CancellationToken cancellationToken)
        {
            var bus = await _context.Buses.SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken);

            var busUpdate = _mapper.Map(request, bus);
            _context.Buses.Attach(busUpdate);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map(busUpdate, new GetBusQueryResult());
        }
    }
}
