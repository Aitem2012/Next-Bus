using System;
using AutoMapper;
using MediatR;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain.Buses;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Buses.Models.Result;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Buses.Handlers.Commands
{
    public class CreateBusCommandHandler : IRequestHandler<CreateBusCommand, GetBusQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public CreateBusCommandHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<GetBusQueryResult> Handle(CreateBusCommand request, CancellationToken cancellationToken)
        {
            var bus = _mapper.Map<Bus>(request);
            bus.DateCreated = DateTime.UtcNow;
            bus.DateUpdated = DateTime.UtcNow;
            await _context.Buses.AddAsync(bus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map(bus, new GetBusQueryResult());
        }
    }
}
