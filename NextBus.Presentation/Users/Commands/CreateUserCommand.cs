using System;
using MediatR;
using NextBus.Presentation.Core;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Users.Commands
{
     public class CreateUserCommand : IRequest<Result<GetUserQueryResult>>
     {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string BVN { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string PickupPoint { get; set; }
        public string Destination { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
     }
}