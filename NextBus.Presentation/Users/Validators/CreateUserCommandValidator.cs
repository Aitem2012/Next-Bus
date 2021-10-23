using FluentValidation;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Commands;
using System.Linq;

namespace NextBus.Presentation.Users.Validators
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
     {
         private readonly IAppDbContext _context;

         public CreateUserCommandValidator(IAppDbContext context)
          {
              _context = context;

              RuleFor(e => e.Firstname).EmailAddress().NotEmpty().NotNull().WithMessage("Firstname cannot be null or empty");
               RuleFor(e => e.Lastname).NotEmpty().NotNull().WithMessage("Lastname cannot be null or empty");
               RuleFor(e => e.Email).NotEmpty().NotNull().WithMessage("Email cannot be null or empty");
               RuleFor(e => e.BVN).NotEmpty().NotNull().Must(HasValidBvn).WithMessage("BVN cannot be null or empty");
               RuleFor(e => e.PhoneNumber).NotEmpty().NotNull().WithMessage("Phone Number cannot be null or empty");
               RuleFor(e => e.Password).NotEmpty().NotNull().WithMessage("Password cannot be null or empty");
          }

          private bool HasValidBvn(CreateUserCommand model, string bvn)
          {
              var user = _context.AppUsers.Single(x => x.BVN == model.BVN);
              return user.BVN == bvn;
          }
     }
}