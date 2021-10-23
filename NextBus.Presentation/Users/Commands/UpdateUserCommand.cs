using System;
using MediatR;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Users.Commands
{
     public class UpdateUserCommand : IRequest<GetUserQueryResult>
     {
          public string Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string PickupPoint { get; set; }
        public string Destination { get; set; }
    }
}