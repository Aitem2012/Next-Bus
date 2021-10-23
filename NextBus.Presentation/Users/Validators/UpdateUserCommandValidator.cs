using FluentValidation;
using NextBus.Presentation.Users.Commands;

namespace NextBus.Presentation.Users.Validators
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
     {
          public UpdateUserCommandValidator()
          {
               RuleFor(e => e.Id).NotEmpty().NotNull().WithMessage("Id cannot be null or empty");

               RuleFor(e => e.Firstname).EmailAddress().NotEmpty().NotNull().WithMessage("Firstname cannot be null or empty");
               RuleFor(e => e.Lastname).NotEmpty().NotNull().WithMessage("Lastname cannot be null or empty");
          }
     }
}
