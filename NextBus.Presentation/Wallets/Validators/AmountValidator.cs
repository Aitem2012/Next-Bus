using FluentValidation.Validators;
using NextBus.Persistence.Context;
using System.Linq;

namespace NextBus.Presentation.Wallets.Validators
{
    public class AmountValidator : PropertyValidator
     {
          private readonly string _walletId;

          public AmountValidator(string walletId)
          {
               _walletId = walletId;
          }

          protected override bool IsValid(PropertyValidatorContext context)
          {
               var db = new AppDbContext();

               var wallet = db.Wallets.Single(e => e.AppUserId.Equals(_walletId));

               return wallet.Balance <= (decimal)context.PropertyValue;
          }

          protected override string GetDefaultMessageTemplate()
          {
               return "{PropertyValue} can not be less than or equal to wallet balance";
          }
     }
}
