using FluentValidation;
using NextBus.Presentation.Drivers.Commands;

namespace NextBus.Presentation.Drivers.Validators
{
    public class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
    {
        public CreateDriverCommandValidator()
        {
            RuleFor(e => e.DriverIdentificationNumber).NotEmpty().NotNull()
                .WithMessage("Driver Identification number is required");
        }
    }
}
