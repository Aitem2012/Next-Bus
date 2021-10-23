using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class ResetPhoneCodeCommand : IRequest<string>
     {
          public string Username { get; set; }
     }
}