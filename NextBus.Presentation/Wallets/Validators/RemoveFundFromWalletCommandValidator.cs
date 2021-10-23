using FluentValidation;
using NextBus.Presentation.Wallets.Command;

namespace NextBus.Presentation.Wallets.Validators
{
    public class RemoveFundFromWalletCommandValidator : AbstractValidator<RemoveFundFromWalletCommand>
     {
          public RemoveFundFromWalletCommandValidator()
          {
               //ToDo: Add a Custom Validator to make sure that money can only be retrieved if the user has funds in their wallet
               RuleFor(e => e.Amount).NotEmpty().NotNull().GreaterThanOrEqualTo(0).WithMessage("Amount cannot be less than 1  or equal to 0");
               RuleFor(e => e.AppUserId).NotNull().NotEmpty().WithMessage("AppUserId cannot be empty or null");
          }
     }
}