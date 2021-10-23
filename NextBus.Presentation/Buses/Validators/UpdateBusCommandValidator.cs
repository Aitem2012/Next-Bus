using FluentValidation;
using NextBus.Presentation.Buses.Commands;

namespace NextBus.Presentation.Buses.Validators
{
    public class UpdateBusCommandValidator : AbstractValidator<UpdateBusCommand>
    {
        public UpdateBusCommandValidator()
        {
            RuleFor(e => e.BusNo).NotNull().NotEmpty().WithMessage("Bus number is required");
            RuleFor(e => e.Destination).NotNull().NotEmpty().WithMessage("Bus Destination is required");
            RuleFor(e => e.TakeOffPoint).NotNull().NotEmpty().WithMessage("Bus Take off point is required");
            //RuleFor(e => e.Seat).NotNull().NotEmpty().WithMessage("Bus total number of seat is required");
            RuleFor(e => e.TotalAvailableSeat).NotNull().NotEmpty().WithMessage("Total number of seat is required");
            RuleFor(e => e.IsVacant).NotEmpty().NotNull().WithMessage("Bus vacancy is required");
        }
    }
}
