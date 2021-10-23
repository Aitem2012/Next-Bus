using System.Collections.Generic;
using MediatR;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Users.Queries
{
     public class GetUsersQuery : IRequest<IEnumerable<GetUserQueryResult>>
     {
     }
}