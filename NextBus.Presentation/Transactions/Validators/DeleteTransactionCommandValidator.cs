using FluentValidation;
using NextBus.Presentation.Transactions.Commands;

namespace NextBus.Presentation.Transactions.Validators
{
     public class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
     {
          public DeleteTransactionCommandValidator()
          {
               RuleFor(e => e.Id).NotEmpty().NotNull().WithMessage("Transaction Id is required");
          }
     }
}