using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Transactions.Models.Result;
using NextBus.Presentation.Transactions.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Transactions.Handlers.Queries
{
    public class GetTransactionQueryHandler : IRequestHandler<GetTransactionQuery, GetTransactionQueryResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public GetTransactionQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<GetTransactionQueryResult> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
        {
            var transaction = await _context.Transactions.SingleAsync(e => e.Id.Equals(request.Id), cancellationToken);
            
            return _mapper.Map<GetTransactionQueryResult>(transaction);
        }
    }
}
