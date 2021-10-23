using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Drivers.Commands;
using NextBus.Presentation.Drivers.Models.Results;

namespace NextBus.Presentation.Drivers.Handlers.Commands
{
    public class UpdateDriverCommandHandler : IRequestHandler<UpdateDriverCommand, GetDriverQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public UpdateDriverCommandHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<GetDriverQueryResult> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
        {
            var driverFromDb = await _context.Drivers.SingleAsync(x => x.AppUserId == request.Id, cancellationToken);

            var result = _mapper.Map(request, driverFromDb);

            _context.Drivers.Attach(result);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map(result, new GetDriverQueryResult());
        }
    }
}
