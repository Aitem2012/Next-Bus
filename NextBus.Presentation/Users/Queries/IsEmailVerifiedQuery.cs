using MediatR;

namespace NextBus.Presentation.Users.Queries
{
     public class IsEmailVerifiedQuery : IRequest<bool>
     {
          public string Username { get; set; }
     }
}