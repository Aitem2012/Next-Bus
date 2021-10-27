using System;

namespace NextBus.Presentation.Wallets.Models.Result
{
     public class GetWalletQueryResult
     {
          public string AppUserId { get; set; }
          public string FullName { get; set; }
          //public int Histories { get; set; }
          public decimal Balance { get; set; }
          public DateTime Date { get; set; }
          public DateTime LastUpdated { get; set; }
          public string Reference { get; set; }
     }
}