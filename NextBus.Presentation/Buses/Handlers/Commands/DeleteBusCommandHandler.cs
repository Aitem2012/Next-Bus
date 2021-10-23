using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Buses.Models.Result;
using NextBus.Presentation.Common.Models.Results;

namespace NextBus.Presentation.Buses.Handlers.Commands
{
    public class DeleteBusCommandHandler : IRequestHandler<DeleteBusCommand, DeleteCommandResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public DeleteBusCommandHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<DeleteCommandResult> Handle(DeleteBusCommand request, CancellationToken cancellationToken)
        {
            var bus = await _context.Buses.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync(cancellationToken);

            return new DeleteCommandResult
            {
                Id = request.Id,
                IsDeleted = true,
                Message = $"Bus with id {request.Id} has been deleted successfully"
            };
        }
    }
}