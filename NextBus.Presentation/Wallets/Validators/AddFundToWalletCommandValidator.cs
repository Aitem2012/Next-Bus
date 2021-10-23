using FluentValidation;
using NextBus.Presentation.Wallets.Command;

namespace NextBus.Presentation.Wallets.Validators
{
     public class AddFundToWalletCommandValidator : AbstractValidator<AddFundToWalletCommand>
     {
          public AddFundToWalletCommandValidator()
          {
               RuleFor(e => e.Amount).NotNull().NotEmpty().GreaterThan(0)
                    .WithMessage("Amount cannot be less than 1  or equal to 0");
               RuleFor(e => e.AppUserId).NotNull().NotEmpty().WithMessage("AppUserId cannot be empty or null");
          }
     }
}