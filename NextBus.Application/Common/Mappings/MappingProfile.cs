using AutoMapper;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;

namespace NextBus.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AppUser, UserDto>();

            CreateMap<Vehicle, VehicleDto>();
            CreateMap<CreateVehicleDto, Vehicle>();

            CreateMap<Ride, RideDto>()
                .ForMember(d => d.DriverName, opt => opt.MapFrom(s => s.Driver != null ? $"{s.Driver.Firstname} {s.Driver.Lastname}".Trim() : string.Empty))
                .ForMember(d => d.DriverPhone, opt => opt.MapFrom(s => s.Driver != null ? s.Driver.PhoneNumber : string.Empty))
                .ForMember(d => d.VehicleDescription, opt => opt.MapFrom(s => s.Vehicle != null ? $"{s.Vehicle.Year} {s.Vehicle.Make} {s.Vehicle.Model} ({s.Vehicle.Color})" : string.Empty))
                .ForMember(d => d.PlateNumber, opt => opt.MapFrom(s => s.Vehicle != null ? s.Vehicle.PlateNumber : string.Empty))
                .ForMember(d => d.VehicleType, opt => opt.MapFrom(s => s.Vehicle != null ? s.Vehicle.VehicleType : Domain.Enums.VehicleType.PrivateCar));

            CreateMap<CreateRideDto, Ride>()
                .ForMember(d => d.TotalSeatsOffered, opt => opt.MapFrom(s => s.AvailableSeats));

            CreateMap<RideBooking, RideBookingDto>()
                .ForMember(d => d.PassengerName, opt => opt.MapFrom(s => s.Passenger != null ? $"{s.Passenger.Firstname} {s.Passenger.Lastname}".Trim() : string.Empty))
                .ForMember(d => d.PassengerPhone, opt => opt.MapFrom(s => s.Passenger != null ? s.Passenger.PhoneNumber : string.Empty))
                .ForMember(d => d.Origin, opt => opt.MapFrom(s => s.Ride != null ? s.Ride.Origin : string.Empty))
                .ForMember(d => d.Destination, opt => opt.MapFrom(s => s.Ride != null ? s.Ride.Destination : string.Empty))
                .ForMember(d => d.DepartureTime, opt => opt.MapFrom(s => s.Ride != null ? s.Ride.DepartureTime : default));

            CreateMap<Wallet, WalletDto>()
                .ForMember(d => d.RecentHistories, opt => opt.MapFrom(s => s.Histories));

            CreateMap<WalletHistory, WalletHistoryDto>();
        }
    }
}
