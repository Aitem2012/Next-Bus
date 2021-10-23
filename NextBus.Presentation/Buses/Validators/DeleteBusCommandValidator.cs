using FluentValidation;
using NextBus.Presentation.Buses.Commands;

namespace NextBus.Presentation.Buses.Validators
{
    public class DeleteBusCommandValidator : AbstractValidator<DeleteBusCommand>
    {
        public DeleteBusCommandValidator()
        {
            RuleFor(e => e.Id).NotNull().NotEmpty().WithMessage("Bus ID is required");
        }
    }
}
