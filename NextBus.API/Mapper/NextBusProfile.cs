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
            CreateMap<AppUser, GetDriverQueryResult>();

            CreateMap<Wallet, GetWalletQueryResult>()
                .ForMember(e => e.FullName, opt => opt.MapFrom(s => $"{s.AppUser.Firstname} {s.AppUser.Lastname}"));

            CreateMap<CreateDriverCommand, Bus>();
            CreateMap<AddFundToWalletCommand, WalletHistory>();
            CreateMap<RemoveFundFromWalletCommand, WalletHistory>();
            CreateMap<WalletHistory, GetWalletHistoryQueryResult>();
            CreateMap<CreateDriverCommand, Driver>().ReverseMap();
            CreateMap<Driver, AppUser>().ReverseMap();
            CreateMap<UpdateDriverCommand, Driver>().ReverseMap();
            CreateMap<Driver, GetDriverQueryResult>()
                .ForMember(e => e.Firstname, opt => opt.MapFrom(s => s.AppUser.Firstname))
                .ForMember(e=> e.Lastname, opt => opt.MapFrom(s => s.AppUser.Lastname))
                .ForMember(e => e.Id, opt => opt.MapFrom(s => s.AppUser.Id))
                .ForMember(e => e.DriverIdentificationNumber, opt => opt.MapFrom(s => s.DriverIdentificationNumber));
            CreateMap<CreateBusCommand, Bus>().ReverseMap();
            CreateMap<UpdateBusCommand, Bus>();
            CreateMap<Bus, GetBusQueryResult>();
                //.ForMember(e => e.DriverName, opt => opt.MapFrom(s => $"{s.Driver.AppUser.Firstname} {s.Driver.AppUser.Lastname}"));
        }
        
    }
}
