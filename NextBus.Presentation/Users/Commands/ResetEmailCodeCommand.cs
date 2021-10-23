using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class ResetEmailCodeCommand : IRequest<string>
     {
          public string Username { get; set; }
     }
}