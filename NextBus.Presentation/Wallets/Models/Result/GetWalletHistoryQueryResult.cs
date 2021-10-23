using System;

namespace NextBus.Presentation.Wallets.Models.Result
{
     public class GetWalletHistoryQueryResult
     {
          public Guid Id { get; set; }
          public decimal Amount { get; set; }
          public string Type { get; set; }
          public DateTime Date { get; set; }
          public string Reference { get; set; }
          public string Details { get; set; }
          public string WalletAppUserId { get; set; }
     }
}