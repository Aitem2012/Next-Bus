using FluentValidation;
using NextBus.Presentation.Transactions.Commands;

namespace NextBus.Presentation.Transactions.Validators
{
     public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
     {
          public CreateTransactionCommandValidator()
          {
               RuleFor(e => e.Amount).NotNull().GreaterThan(0).WithMessage("Amount cannot be less than or 0");
               RuleFor(e => e.AppUserId).NotNull().WithMessage("App User Id should be provided");
          }
     }
}