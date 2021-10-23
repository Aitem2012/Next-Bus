using MediatR;

namespace NextBus.Presentation.Users.Queries
{
     public class IsUserExistQuery : IRequest<bool>
     {
          public string Username { get; set; }
     }
}
