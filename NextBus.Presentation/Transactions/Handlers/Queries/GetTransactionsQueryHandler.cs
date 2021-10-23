using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Transactions.Models.Result;
using NextBus.Presentation.Transactions.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Transactions.Handlers.Queries
{
    public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, IEnumerable<GetTransactionQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        public GetTransactionsQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
        }
        public async Task<IEnumerable<GetTransactionQueryResult>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
        {
            var transactions = await _context.Transactions.ToListAsync(cancellationToken);
            return _mapper.Map<IEnumerable<GetTransactionQueryResult>>(transactions);
        }
    }
}
