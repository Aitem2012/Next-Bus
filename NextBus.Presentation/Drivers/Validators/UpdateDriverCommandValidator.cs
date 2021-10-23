using FluentValidation;
using NextBus.Presentation.Drivers.Commands;

namespace NextBus.Presentation.Drivers.Validators
{
    public class UpdateDriverCommandValidator : AbstractValidator<UpdateDriverCommand>
    {
        public UpdateDriverCommandValidator()
        {
            RuleFor(e => e.Id).NotNull().NotEmpty().WithMessage("Driver ID is required");
        }
    }
}
