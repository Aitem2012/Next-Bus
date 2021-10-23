using FluentValidation;
using NextBus.Presentation.Drivers.Commands;

namespace NextBus.Presentation.Drivers.Validators
{
    public class DeleteDriverCommandValidator : AbstractValidator<DeleteDriverCommand>
    {
        public DeleteDriverCommandValidator()
        {
            RuleFor(e => e.Id).NotEmpty().NotNull().WithMessage("Driver ID is required");
        }
    }
}
