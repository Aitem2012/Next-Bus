using MediatR;

namespace NextBus.Presentation.Users.Queries
{
     public class IsPhoneVerifiedQuery : IRequest<bool>
     {
          public string Username { get; set; }
     }
}