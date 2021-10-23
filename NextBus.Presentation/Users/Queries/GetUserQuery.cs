using MediatR;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Users.Queries
{
     public class GetUserQuery : IRequest<GetUserQueryResult>
     {
          public string Id { get; set; }
     }
}