using AutoMapper;
using NextBus.Domain.Buses;
using NextBus.Domain.Drivers;
using NextBus.Domain.Users;
using NextBus.Domain.Wallets;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Buses.Models.Result;
using NextBus.Presentation.Drivers.Commands;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using NextBus.Presentation.Wallets.Command;
using NextBus.Presentation.Wallets.Models.Result;

namespace NextBus.API.Mapper
{
    public class NextBusProfile : Profile
    {
        public NextBusProfile()
        {
            CreateMap<CreateUserCommand, AppUser>();
            CreateMap<UpdateUserCommand, AppUser>();
            CreateMap<AppUser, GetUserQueryResult>()
                .ForMember(e => e.Transactions, opt => opt.MapFrom(s => s.Transactions.Count));

            CreateMap<Wallet, GetWalletQueryResult>()
                .ForMember(e => e.FullName, opt => opt.MapFrom(s => $"{s.AppUser.Firstname} {s.AppUser.Lastname}"))
                .ForMember(e => e.Histories, opt => opt.MapFrom(s => s.Histories.Count));

            CreateMap<AddFundToWalletCommand, WalletHistory>();
            CreateMap<RemoveFundFromWalletCommand, WalletHistory>();
            CreateMap<WalletHistory, GetWalletHistoryQueryResult>();
            CreateMap<CreateDriverCommand, Driver>().ReverseMap();
            CreateMap<Driver, AppUser>().ReverseMap();
            CreateMap<UpdateDriverCommand, Driver>().ReverseMap();
            CreateMap<Driver, GetDriverQueryResult>().ReverseMap();
            CreateMap<CreateBusCommand, Bus>().ReverseMap();
            CreateMap<UpdateBusCommand, Bus>();
            CreateMap<Bus, GetBusQueryResult>();
        }
        
    }
}
