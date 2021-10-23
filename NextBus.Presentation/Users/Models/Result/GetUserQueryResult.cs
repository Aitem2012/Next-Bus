using System;

namespace NextBus.Presentation.Users.Models.Result
{
     public class GetUserQueryResult
     { 
         public string Id { get; set; }
         public string Firstname { get; set; }
         public string Lastname { get; set; }
         public string PickupPoint { get; set; }
         public string Destination { get; set; }
         public string Email { get; set; }
         public string PhoneNumber { get; set; }
         public int Transactions { get; set; }
     }
}