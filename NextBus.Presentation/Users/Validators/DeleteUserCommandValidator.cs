using FluentValidation;
using NextBus.Presentation.Users.Commands;

namespace NextBus.Presentation.Users.Validators
{
     public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
     {
          public DeleteUserCommandValidator()
          {
               RuleFor(e => e.Id).NotEmpty().NotNull().WithMessage("Id cannot be null or empty");
          }
     }
}
