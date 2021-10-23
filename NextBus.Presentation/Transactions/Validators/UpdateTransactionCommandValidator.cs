using FluentValidation;
using NextBus.Presentation.Transactions.Commands;

namespace NextBus.Presentation.Transactions.Validators
{
    public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
     {
          public UpdateTransactionCommandValidator()
          {
               RuleFor(e => e.Amount).GreaterThan(0).NotNull().WithMessage("Amount cannot be less than or 0");
               RuleFor(e => e.AppUserId).NotNull().WithMessage("App User Id should be provided");
          }

     }
}