using System;
using MediatR;
using NextBus.Presentation.Buses.Models.Result;

namespace NextBus.Presentation.Buses.Queries
{
    public class GetBusQuery : IRequest<GetBusQueryResult>
    {
        public Guid Id { get; set; }   
    }
}
