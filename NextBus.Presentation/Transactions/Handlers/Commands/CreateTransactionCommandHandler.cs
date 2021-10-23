using AutoMapper;
using MediatR;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain.Transactions;
using NextBus.Presentation.Transactions.Commands;
using NextBus.Presentation.Transactions.Models.Result;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Transactions.Handlers.Commands
{
    public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionCommandResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public CreateTransactionCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<CreateTransactionCommandResult> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
          {
               var transaction = _mapper.Map<Transaction>(request);
               transaction.Date = DateTime.Now;

               await _context.Transactions.AddAsync(transaction, cancellationToken);
               await _context.SaveChangesAsync(cancellationToken);
               

               return _mapper.Map(transaction, new CreateTransactionCommandResult());
          }
     }
}
