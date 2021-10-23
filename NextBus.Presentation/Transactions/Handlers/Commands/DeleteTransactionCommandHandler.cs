using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Transactions.Commands;
using NextBus.Presentation.Transactions.Models.Result;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Transactions.Handlers.Commands
{
    public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, DeleteTransactionCommandResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public DeleteTransactionCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<DeleteTransactionCommandResult> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
        {
            var transactionFromDb = await _context.Transactions.SingleAsync(e => e.Id.Equals(request.Id), cancellationToken);
            _context.Transactions.Remove(transactionFromDb);
            await _context.SaveChangesAsync(cancellationToken);
            
            return new DeleteTransactionCommandResult
            {
                Id = request.Id,
                IsDeleted = true,
                Message = $"Transaction with id { request.Id } is successfully deleted"
            };
        }
    }
}
