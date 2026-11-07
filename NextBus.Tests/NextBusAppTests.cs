using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Mappings;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Bookings;
using NextBus.Application.Features.Rides;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;
using NextBus.Persistence.Context;
using Xunit;

namespace NextBus.Tests
{
    public class NextBusAppTests
    {
        private readonly IMapper _mapper;

        public NextBusAppTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
        }

        private static AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source=file:memdb_{Guid.NewGuid():N}?mode=memory&cache=shared")
                .Options;

            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task SearchRides_MatchesOriginDestinationAndStops()
        {
            using var context = CreateInMemoryDbContext();

            var driver = new AppUser
            {
                Id = "driver-1",
                Firstname = "Emeka",
                Lastname = "Okonkwo",
                Email = "emeka@test.com",
                DriverKycStatus = DriverKycStatus.Verified
            };
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Make = "Toyota",
                Model = "Corolla",
                PlateNumber = "ABC-123-XY",
                TotalSeats = 4
            };

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                Origin = "Ikeja",
                Destination = "Victoria Island",
                Stop1 = "Maryland",
                Stop2 = "Yaba",
                DepartureTime = DateTime.UtcNow.AddHours(2),
                TotalSeatsOffered = 3,
                AvailableSeats = 3,
                PricePerSeat = 1500m,
                Status = RideStatus.Scheduled
            };

            await context.Users.AddAsync(driver);
            await context.Vehicles.AddAsync(vehicle);
            await context.Rides.AddAsync(ride);
            await context.SaveChangesAsync();

            var handler = new SearchRidesQueryHandler(context, _mapper);

            // 1. Search by Stop1 ("Maryland") and Destination ("Victoria Island")
            var search1 = await handler.Handle(new SearchRidesQuery(new RideSearchQueryDto
            {
                Origin = "Maryland",
                Destination = "Victoria Island"
            }), CancellationToken.None);

            Assert.True(search1.Succeeded);
            Assert.Single(search1.Data!);
            Assert.Equal("Ikeja", search1.Data![0].Origin);
            Assert.Equal("Maryland", search1.Data![0].Stop1);

            // 2. Search by Stop2 ("Yaba")
            var search2 = await handler.Handle(new SearchRidesQuery(new RideSearchQueryDto
            {
                Destination = "Yaba"
            }), CancellationToken.None);

            Assert.True(search2.Succeeded);
            Assert.Single(search2.Data!);

            // 3. Search non-matching destination
            var search3 = await handler.Handle(new SearchRidesQuery(new RideSearchQueryDto
            {
                Destination = "Abuja"
            }), CancellationToken.None);

            Assert.True(search3.Succeeded);
            Assert.Empty(search3.Data!);
        }

        [Fact]
        public async Task BookingRide_RequiresKycAndSufficientWalletBalance()
        {
            using var context = CreateInMemoryDbContext();

            var driver = new AppUser
            {
                Id = "driver-1",
                Firstname = "Dayo",
                Lastname = "Ade",
                Email = "dayo@test.com",
                DriverKycStatus = DriverKycStatus.Verified
            };

            var unverifiedPassenger = new AppUser
            {
                Id = "passenger-unverified",
                Firstname = "Chioma",
                Lastname = "Nwosu",
                Email = "chioma@test.com",
                KycStatus = KycStatus.NotSubmitted // Unverified KYC
            };

            var verifiedPassenger = new AppUser
            {
                Id = "passenger-verified",
                Firstname = "Tunde",
                Lastname = "Bakare",
                Email = "tunde@test.com",
                KycStatus = KycStatus.Verified, // Verified KYC
                Wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    AppUserId = "passenger-verified",
                    Balance = 500m, // Only 500, but ride costs 1500!
                    Reference = "W_TUNDE"
                }
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Make = "Honda",
                Model = "Civic",
                PlateNumber = "APP-456-ZZ",
                TotalSeats = 4
            };

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                Origin = "Lekki",
                Destination = "Ajah",
                DepartureTime = DateTime.UtcNow.AddHours(3),
                TotalSeatsOffered = 3,
                AvailableSeats = 3,
                PricePerSeat = 1500m,
                Status = RideStatus.Scheduled
            };

            await context.Users.AddRangeAsync(driver, unverifiedPassenger, verifiedPassenger);
            await context.Vehicles.AddAsync(vehicle);
            await context.Rides.AddAsync(ride);
            await context.SaveChangesAsync();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var userManagerMock = new Mock<UserManager<AppUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(u => u.FindByIdAsync("passenger-unverified")).ReturnsAsync(unverifiedPassenger);
            userManagerMock.Setup(u => u.FindByIdAsync("passenger-verified")).ReturnsAsync(verifiedPassenger);

            var currentUserServiceMock = new Mock<ICurrentUserService>();

            // Case 1: Unverified KYC fails
            currentUserServiceMock.Setup(c => c.UserId).Returns("passenger-unverified");
            var handler = new RequestRideBookingCommandHandler(context, userManagerMock.Object, currentUserServiceMock.Object, _mapper);

            var result1 = await handler.Handle(new RequestRideBookingCommand(new CreateBookingDto
            {
                RideId = ride.Id,
                PickupLocation = "Lekki",
                DropoffLocation = "Ajah",
                SeatsBooked = 1
            }), CancellationToken.None);

            Assert.False(result1.Succeeded);
            Assert.Contains("KYC", result1.Errors[0]);

            // Case 2: Verified KYC but Insufficient Wallet balance (Balance 500 < Fare 1500)
            currentUserServiceMock.Setup(c => c.UserId).Returns("passenger-verified");

            var result2 = await handler.Handle(new RequestRideBookingCommand(new CreateBookingDto
            {
                RideId = ride.Id,
                PickupLocation = "Lekki",
                DropoffLocation = "Ajah",
                SeatsBooked = 1
            }), CancellationToken.None);

            Assert.False(result2.Succeeded);
            Assert.Contains("Insufficient wallet balance", result2.Errors[0]);

            // Case 3: Passenger tops up wallet to 2000 and successfully books
            verifiedPassenger.Wallet.Balance = 2000m;
            await context.SaveChangesAsync();

            var result3 = await handler.Handle(new RequestRideBookingCommand(new CreateBookingDto
            {
                RideId = ride.Id,
                PickupLocation = "Lekki",
                DropoffLocation = "Ajah",
                SeatsBooked = 1
            }), CancellationToken.None);

            Assert.True(result3.Succeeded);
            Assert.Equal(500m, verifiedPassenger.Wallet.Balance); // 2000 - 1500 = 500

            // Case 4: Driver accepts booking -> seats decrement to 2
            var booking = await context.RideBookings.FirstAsync();
            currentUserServiceMock.Setup(c => c.UserId).Returns("driver-1");

            var respondHandler = new RespondToBookingCommandHandler(context, currentUserServiceMock.Object, _mapper);
            var respondResult = await respondHandler.Handle(new RespondToBookingCommand(booking.Id, new RespondBookingDto
            {
                Accept = true
            }), CancellationToken.None);

            Assert.True(respondResult.Succeeded);
            var updatedRide = await context.Rides.FindAsync(ride.Id);
            Assert.Equal(2, updatedRide!.AvailableSeats); // 3 - 1 = 2 seats remaining
        }
    }
}
