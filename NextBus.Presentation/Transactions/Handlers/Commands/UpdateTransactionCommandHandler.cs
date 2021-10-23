using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Transactions.Commands;
using NextBus.Presentation.Transactions.Models.Result;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Transactions.Handlers.Commands
{
    public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, UpdateTransactionCommandResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public UpdateTransactionCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<UpdateTransactionCommandResult> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
        {
            var transactionInDb = await _context.Transactions.SingleAsync(e => e.Id.Equals(request.Id), cancellationToken);
            var transaction = _mapper.Map(request, transactionInDb);

            
            _context.Transactions.Attach(transaction);
            await _context.SaveChangesAsync(cancellationToken);
            

            return _mapper.Map(transaction, new UpdateTransactionCommandResult());
        }
    }
}
