using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextBus.Persistence;
using Moq;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Mappings;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Auth;
using NextBus.Application.Features.Bookings;
using NextBus.Application.Features.Rides;
using NextBus.Application.Features.Wallets;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;
using NextBus.Infrastructure.Services;
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
            var notificationServiceMock = new Mock<INotificationService>();
            var emailServiceMock = new Mock<IEmailService>();

            // Case 1: Unverified KYC fails
            currentUserServiceMock.Setup(c => c.UserId).Returns("passenger-unverified");
            var handler = new RequestRideBookingCommandHandler(
                context, 
                userManagerMock.Object, 
                currentUserServiceMock.Object, 
                notificationServiceMock.Object, 
                emailServiceMock.Object, 
                _mapper);

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

            var respondHandler = new RespondToBookingCommandHandler(
                context, 
                currentUserServiceMock.Object, 
                notificationServiceMock.Object, 
                _mapper);
            var respondResult = await respondHandler.Handle(new RespondToBookingCommand(booking.Id, new RespondBookingDto
            {
                Accept = true
            }), CancellationToken.None);

            Assert.True(respondResult.Succeeded);
            var updatedRide = await context.Rides.FindAsync(ride.Id);
            Assert.Equal(2, updatedRide!.AvailableSeats); // 3 - 1 = 2 seats remaining
        }

        [Fact]
        public async Task EmailVerificationOtp_Flow_WorksCorrectly()
        {
            using var context = CreateInMemoryDbContext();

            var emailServiceMock = new Mock<IEmailService>();
            var otpService = new OtpService(context, emailServiceMock.Object);

            var user = new AppUser
            {
                Id = "user-otp-1",
                UserName = "newuser@test.com",
                Email = "newuser@test.com",
                Firstname = "John",
                Lastname = "Doe",
                EmailConfirmed = false
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // 1. Generate OTP
            var generatedOtp = await otpService.GenerateAndSendOtpAsync(user.Email, user.Id, OtpPurpose.EmailVerification);
            Assert.Equal(6, generatedOtp.Length);

            // 2. Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var userManagerMock = new Mock<UserManager<AppUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var jwtMock = new Mock<IJwtTokenService>();
            jwtMock.Setup(j => j.GenerateToken(It.IsAny<AppUser>(), It.IsAny<IList<string>>())).Returns("mock_jwt_token");

            var verifyHandler = new VerifyEmailOtpCommandHandler(userManagerMock.Object, otpService, jwtMock.Object, _mapper);

            // Invalid OTP should fail
            var invalidResult = await verifyHandler.Handle(new VerifyEmailOtpCommand(new VerifyEmailOtpDto
            {
                Email = user.Email,
                OtpCode = "000000"
            }), CancellationToken.None);

            Assert.False(invalidResult.Succeeded);
            Assert.False(user.EmailConfirmed);

            // Valid OTP should succeed
            var validResult = await verifyHandler.Handle(new VerifyEmailOtpCommand(new VerifyEmailOtpDto
            {
                Email = user.Email,
                OtpCode = generatedOtp
            }), CancellationToken.None);

            Assert.True(validResult.Succeeded);
            Assert.True(user.EmailConfirmed);
            Assert.Equal("mock_jwt_token", validResult.Data!.Token);
        }

        [Fact]
        public async Task PasswordResetOtp_Flow_WorksCorrectly()
        {
            using var context = CreateInMemoryDbContext();

            var emailServiceMock = new Mock<IEmailService>();
            var otpService = new OtpService(context, emailServiceMock.Object);

            var user = new AppUser
            {
                Id = "user-pwd-1",
                UserName = "resetme@test.com",
                Email = "resetme@test.com",
                Firstname = "Jane",
                Lastname = "Smith"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var userManagerMock = new Mock<UserManager<AppUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token_123");
            userManagerMock.Setup(u => u.ResetPasswordAsync(user, "token_123", "NewSecret123!")).ReturnsAsync(IdentityResult.Success);

            // 1. Request Password Reset OTP
            var requestHandler = new RequestPasswordResetOtpCommandHandler(userManagerMock.Object, otpService);
            var reqResult = await requestHandler.Handle(new RequestPasswordResetOtpCommand(new RequestPasswordResetDto
            {
                Email = user.Email
            }), CancellationToken.None);

            Assert.True(reqResult.Succeeded);

            // Find generated OTP in DB
            var dbOtp = await context.OtpVerifications.FirstAsync(o => o.Email == user.Email && o.Purpose == OtpPurpose.PasswordReset);

            // 2. Reset Password with OTP
            var resetHandler = new ResetPasswordWithOtpCommandHandler(userManagerMock.Object, otpService);
            var resetResult = await resetHandler.Handle(new ResetPasswordWithOtpCommand(new ResetPasswordWithOtpDto
            {
                Email = user.Email,
                OtpCode = dbOtp.Code,
                NewPassword = "NewSecret123!"
            }), CancellationToken.None);

            Assert.True(resetResult.Succeeded);

            // OTP should now be marked as used
            var usedOtp = await context.OtpVerifications.FindAsync(dbOtp.Id);
            Assert.True(usedOtp!.IsUsed);
        }

        [Fact]
        public async Task PaystackWalletFunding_AndWithdrawalWithOtp_WorksCorrectly()
        {
            using var context = CreateInMemoryDbContext();

            var user = new AppUser
            {
                Id = "user-wallet-1",
                UserName = "walletuser@test.com",
                Email = "walletuser@test.com",
                Firstname = "Femi",
                Lastname = "Adekunle"
            };
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                Balance = 0m,
                Reference = "W_FEMI"
            };
            await context.Users.AddAsync(user);
            await context.Wallets.AddAsync(wallet);
            await context.SaveChangesAsync();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var userManagerMock = new Mock<UserManager<AppUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(c => c.UserId).Returns(user.Id);

            var paystackMock = new Mock<IPaystackService>();
            var emailServiceMock = new Mock<IEmailService>();
            var otpService = new OtpService(context, emailServiceMock.Object);

            // 1. Paystack Top-Up Verification
            var topUpReference = "PAY_REF_1001";
            paystackMock.Setup(p => p.VerifyPaymentAsync(topUpReference)).ReturnsAsync(Result<PaystackVerifyResponseDto>.Success(new PaystackVerifyResponseDto
            {
                IsSuccessful = true,
                Amount = 10000m,
                Reference = topUpReference,
                Channel = "card",
                Currency = "NGN",
                CustomerEmail = user.Email
            }));

            var verifyTopUpHandler = new VerifyPaystackTopUpCommandHandler(context, currentUserServiceMock.Object, paystackMock.Object, _mapper);
            var topUpResult = await verifyTopUpHandler.Handle(new VerifyPaystackTopUpCommand(new VerifyFundWalletDto
            {
                Reference = topUpReference
            }), CancellationToken.None);

            Assert.True(topUpResult.Succeeded);
            Assert.Equal(10000m, wallet.Balance);

            // Verify idempotency (prevent double credit)
            var doubleCreditResult = await verifyTopUpHandler.Handle(new VerifyPaystackTopUpCommand(new VerifyFundWalletDto
            {
                Reference = topUpReference
            }), CancellationToken.None);

            Assert.True(doubleCreditResult.Succeeded);
            Assert.Equal(10000m, wallet.Balance); // Balance remains 10000m

            // 2. Withdrawal Step 1: Request OTP with Destination Account
            paystackMock.Setup(p => p.ResolveAccountNumberAsync("0123456789", "058"))
                .ReturnsAsync(Result<PaystackResolveAccountDto>.Success(new PaystackResolveAccountDto
                {
                    AccountNumber = "0123456789",
                    AccountName = "FEMI ADEKUNLE",
                    BankId = 9
                }));

            var requestWithdrawalHandler = new RequestWithdrawalOtpCommandHandler(
                context, currentUserServiceMock.Object, userManagerMock.Object, paystackMock.Object, otpService);

            var wdrwReqResult = await requestWithdrawalHandler.Handle(new RequestWithdrawalOtpCommand(new RequestWithdrawalDto
            {
                Amount = 4000m,
                AccountNumber = "0123456789",
                BankCode = "058",
                AccountName = "FEMI ADEKUNLE"
            }), CancellationToken.None);

            Assert.True(wdrwReqResult.Succeeded);
            Assert.Contains("FEMI ADEKUNLE", wdrwReqResult.Message!);

            var withdrawalOtp = await context.OtpVerifications.FirstAsync(o => o.Email == user.Email && o.Purpose == OtpPurpose.Withdrawal);

            // 3. Withdrawal Step 2: Confirm OTP
            paystackMock.Setup(p => p.CreateTransferRecipientAsync("FEMI ADEKUNLE", "0123456789", "058"))
                .ReturnsAsync(Result<PaystackRecipientResponseDto>.Success(new PaystackRecipientResponseDto
                {
                    RecipientCode = "RCP_test_123",
                    Name = "FEMI ADEKUNLE"
                }));

            paystackMock.Setup(p => p.InitiateTransferAsync(4000m, "RCP_test_123", "NextBus Wallet Withdrawal", It.IsAny<string>()))
                .ReturnsAsync(Result<PaystackTransferResponseDto>.Success(new PaystackTransferResponseDto
                {
                    TransferCode = "TRF_999",
                    Status = "success"
                }));

            var confirmWithdrawalHandler = new ConfirmWithdrawalCommandHandler(
                context, currentUserServiceMock.Object, userManagerMock.Object, paystackMock.Object, otpService, _mapper);

            var confirmResult = await confirmWithdrawalHandler.Handle(new ConfirmWithdrawalCommand(new ConfirmWithdrawalDto
            {
                OtpCode = withdrawalOtp.Code
            }), CancellationToken.None);

            Assert.True(confirmResult.Succeeded);
            Assert.Equal(6000m, wallet.Balance); // 10000 - 4000 = 6000

            var withdrawalTx = await context.Transactions.FirstOrDefaultAsync(t => t.Type == TransactionType.Withdrawal);
            Assert.NotNull(withdrawalTx);
            Assert.Equal(4000m, withdrawalTx.Amount);
        }

        [Fact]
        public async Task CompleteRide_DeductsPlatformCommission_ForEachPassenger()
        {
            using var context = CreateInMemoryDbContext();

            var driver = new AppUser
            {
                Id = "driver-comm",
                Firstname = "Babatunde",
                Lastname = "Raji",
                Email = "babatunde@test.com"
            };
            var driverWallet = new Wallet
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Balance = 0m,
                Reference = "W_DRIVER"
            };

            var passenger1 = new AppUser { Id = "p-1", Firstname = "P1", Lastname = "One", Email = "p1@test.com" };
            var passenger2 = new AppUser { Id = "p-2", Firstname = "P2", Lastname = "Two", Email = "p2@test.com" };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Make = "Toyota",
                Model = "HiAce",
                PlateNumber = "EPE-999-LK",
                TotalSeats = 14
            };

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                VehicleId = vehicle.Id,
                Origin = "Oshodi",
                Destination = "Ajah",
                DepartureTime = DateTime.UtcNow.AddMinutes(10),
                TotalSeatsOffered = 10,
                AvailableSeats = 7,
                PricePerSeat = 1000m,
                Status = RideStatus.InProgress
            };

            // Passenger 1 booked 1 seat = 1000 NGN
            var booking1 = new RideBooking
            {
                Id = Guid.NewGuid(),
                RideId = ride.Id,
                PassengerId = passenger1.Id,
                PickupLocation = "Oshodi",
                DropoffLocation = "Ajah",
                SeatsBooked = 1,
                PricePerSeat = 1000m,
                TotalFare = 1000m,
                BookingStatus = BookingStatus.Accepted,
                PaymentStatus = PaymentStatus.Paid
            };

            // Passenger 2 booked 2 seats = 2000 NGN
            var booking2 = new RideBooking
            {
                Id = Guid.NewGuid(),
                RideId = ride.Id,
                PassengerId = passenger2.Id,
                PickupLocation = "Oshodi",
                DropoffLocation = "Ajah",
                SeatsBooked = 2,
                PricePerSeat = 1000m,
                TotalFare = 2000m,
                BookingStatus = BookingStatus.Accepted,
                PaymentStatus = PaymentStatus.Paid
            };

            await context.Users.AddRangeAsync(driver, passenger1, passenger2);
            await context.Wallets.AddAsync(driverWallet);
            await context.Vehicles.AddAsync(vehicle);
            await context.Rides.AddAsync(ride);
            await context.RideBookings.AddRangeAsync(booking1, booking2);
            await context.SaveChangesAsync();

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(c => c.UserId).Returns(driver.Id);

            // 10% platform commission configured
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "PlatformSettings:CommissionPercentage", "10.0" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var notifMock = new Mock<INotificationService>();
            var completeHandler = new CompleteRideCommandHandler(context, currentUserServiceMock.Object, configuration, notifMock.Object);
            var result = await completeHandler.Handle(new CompleteRideCommand(ride.Id), CancellationToken.None);

            Assert.True(result.Succeeded);

            // Verify Booking 1: Total 1000 -> 10% Commission (100) -> Driver Earnings (900)
            var b1 = await context.RideBookings.FindAsync(booking1.Id);
            Assert.Equal(100m, b1!.CommissionAmount);
            Assert.Equal(900m, b1.DriverEarnings);
            Assert.Equal(BookingStatus.Completed, b1.BookingStatus);

            // Verify Booking 2: Total 2000 -> 10% Commission (200) -> Driver Earnings (1800)
            var b2 = await context.RideBookings.FindAsync(booking2.Id);
            Assert.Equal(200m, b2!.CommissionAmount);
            Assert.Equal(1800m, b2.DriverEarnings);
            Assert.Equal(BookingStatus.Completed, b2.BookingStatus);

            // Verify Driver Wallet credited with net total: 900 + 1800 = 2700 NGN
            var updatedDriverWallet = await context.Wallets.FindAsync(driverWallet.Id);
            Assert.Equal(2700m, updatedDriverWallet!.Balance);

            // Verify Platform Commission transactions recorded
            var commissionTransactions = await context.Transactions
                .Where(t => t.Type == TransactionType.PlatformCommission)
                .ToListAsync();
            Assert.Equal(2, commissionTransactions.Count);
            Assert.Equal(300m, commissionTransactions.Sum(t => t.Amount)); // 100 + 200 = 300 total platform commission
        }

        [Fact]
        public async Task CompleteRide_WithCheckedInAndCheckedOutBookings_CreditsDriverWallet()
        {
            using var context = CreateInMemoryDbContext();

            var driver = new AppUser
            {
                Id = "driver-checkedout-1",
                Email = "driver-co@test.com",
                UserName = "driver-co@test.com",
                Firstname = "Driver",
                Lastname = "One"
            };
            var driverWallet = new Wallet
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Balance = 0m,
                Reference = "DRV_W1",
                LastUpdated = DateTime.UtcNow
            };
            await context.Users.AddAsync(driver);
            await context.Wallets.AddAsync(driverWallet);

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Make = "Toyota",
                Model = "Corolla",
                PlateNumber = "LAG-123-BA",
                TotalSeats = 4
            };
            await context.Vehicles.AddAsync(vehicle);

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                VehicleId = vehicle.Id,
                Origin = "Ikorodu",
                Destination = "Ikeja",
                DepartureTime = DateTime.UtcNow.AddHours(1),
                PricePerSeat = 1500m,
                TotalSeatsOffered = 4,
                AvailableSeats = 2,
                Status = RideStatus.InProgress
            };
            await context.Rides.AddAsync(ride);

            var p1 = new AppUser
            {
                Id = "passenger-in",
                Email = "p1@test.com",
                UserName = "p1@test.com",
                Firstname = "Passenger",
                Lastname = "One"
            };
            var p2 = new AppUser
            {
                Id = "passenger-out",
                Email = "p2@test.com",
                UserName = "p2@test.com",
                Firstname = "Passenger",
                Lastname = "Two"
            };
            await context.Users.AddRangeAsync(p1, p2);

            // Passenger 1 has CheckedIn
            var booking1 = new RideBooking
            {
                Id = Guid.NewGuid(),
                RideId = ride.Id,
                PassengerId = "passenger-in",
                PickupLocation = "Ikorodu",
                DropoffLocation = "Maryland",
                SeatsBooked = 1,
                PricePerSeat = 1500m,
                TotalFare = 1500m,
                BookingStatus = BookingStatus.CheckedIn,
                PaymentStatus = PaymentStatus.Paid,
                RequestedAt = DateTime.UtcNow
            };

            // Passenger 2 has CheckedOut
            var booking2 = new RideBooking
            {
                Id = Guid.NewGuid(),
                RideId = ride.Id,
                PassengerId = "passenger-out",
                PickupLocation = "Ikorodu",
                DropoffLocation = "Ikeja",
                SeatsBooked = 1,
                PricePerSeat = 1500m,
                TotalFare = 1500m,
                BookingStatus = BookingStatus.CheckedOut,
                PaymentStatus = PaymentStatus.Paid,
                RequestedAt = DateTime.UtcNow
            };

            await context.RideBookings.AddRangeAsync(booking1, booking2);
            await context.SaveChangesAsync();

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(c => c.UserId).Returns(driver.Id);

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "PlatformSettings:CommissionPercentage", "10.0" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var notifMock = new Mock<INotificationService>();
            var completeHandler = new CompleteRideCommandHandler(context, currentUserServiceMock.Object, configuration, notifMock.Object);
            var result = await completeHandler.Handle(new CompleteRideCommand(ride.Id), CancellationToken.None);

            Assert.True(result.Succeeded);

            // Gross fare = 1500 + 1500 = 3000. 10% commission = 300. Net driver earnings = 2700.
            var updatedWallet = await context.Wallets.FindAsync(driverWallet.Id);
            Assert.Equal(2700m, updatedWallet!.Balance);

            var b1 = await context.RideBookings.FindAsync(booking1.Id);
            Assert.Equal(BookingStatus.Completed, b1!.BookingStatus);
            Assert.Equal(1350m, b1.DriverEarnings);

            var b2 = await context.RideBookings.FindAsync(booking2.Id);
            Assert.Equal(BookingStatus.Completed, b2!.BookingStatus);
            Assert.Equal(1350m, b2.DriverEarnings);
        }

        [Fact]
        public async Task PaystackWebhook_CreditsWalletOnChargeSuccess()
        {
            using var context = CreateInMemoryDbContext();

            var user = new AppUser
            {
                Id = "webhook-user-1",
                Email = "webhook@test.com",
                UserName = "webhook@test.com",
                Firstname = "Sola",
                Lastname = "Shittu"
            };
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                Balance = 1000m,
                Reference = "W_SOLA"
            };

            await context.Users.AddAsync(user);
            await context.Wallets.AddAsync(wallet);
            await context.SaveChangesAsync();

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Paystack:SecretKey", "test_secret_key" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var loggerMock = new Mock<ILogger<ProcessPaystackWebhookCommandHandler>>();
            var handler = new ProcessPaystackWebhookCommandHandler(context, configuration, loggerMock.Object);

            var payload = @"{
                ""event"": ""charge.success"",
                ""data"": {
                    ""reference"": ""T_WEBHOOK_999"",
                    ""amount"": 500000,
                    ""customer"": {
                        ""email"": ""webhook@test.com""
                    }
                }
            }";

            // Compute valid HMAC-SHA512 signature
            using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes("test_secret_key"));
            var signature = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

            // 1. Process valid webhook
            var result = await handler.Handle(new ProcessPaystackWebhookCommand(payload, signature), CancellationToken.None);

            Assert.True(result.Succeeded);
            var updatedWallet = await context.Wallets.FindAsync(wallet.Id);
            Assert.Equal(6000m, updatedWallet!.Balance); // 1000 + 5000 = 6000

            // 2. Process duplicate webhook -> idempotent, does not double-credit
            var dupResult = await handler.Handle(new ProcessPaystackWebhookCommand(payload, signature), CancellationToken.None);
            Assert.True(dupResult.Succeeded);
            Assert.Equal(6000m, updatedWallet.Balance);
        }

        [Fact]
        public async Task Database_PreservesDataAcrossRestarts()
        {
            var testDbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nextbus_restart_test_{Guid.NewGuid():N}.db");
            var connString = $"Data Source={testDbPath}";

            try
            {
                // App Run 1: Startup & Migration
                var services1 = new ServiceCollection();
                services1.AddLogging();
                var config1 = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ConnectionStrings:DefaultConnection", connString }
                    })
                    .Build();

                services1.AddPersistence(config1);
                var provider1 = services1.BuildServiceProvider();

                // Run migration
                await provider1.MigrateDatabaseAsync();

                // Insert record in Run 1
                using (var scope1 = provider1.CreateScope())
                {
                    var ctx1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
                    var user = new AppUser
                    {
                        Id = "persistent-user-1",
                        Email = "persistent@test.com",
                        UserName = "persistent@test.com",
                        Firstname = "Persistent",
                        Lastname = "User"
                    };
                    await ctx1.Users.AddAsync(user);
                    await ctx1.SaveChangesAsync();
                }

                // Simulate app shutdown
                await provider1.DisposeAsync();

                // App Run 2: Restart app pointing to same database
                var services2 = new ServiceCollection();
                services2.AddLogging();
                var config2 = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ConnectionStrings:DefaultConnection", connString }
                    })
                    .Build();

                services2.AddPersistence(config2);
                var provider2 = services2.BuildServiceProvider();

                // Run migration on restart (should NOT override, reset, or throw)
                await provider2.MigrateDatabaseAsync();

                // Verify record from Run 1 is intact
                using (var scope2 = provider2.CreateScope())
                {
                    var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                    var retrievedUser = await ctx2.Users.FindAsync("persistent-user-1");

                    Assert.NotNull(retrievedUser);
                    Assert.Equal("persistent@test.com", retrievedUser.Email);
                    Assert.Equal("Persistent", retrievedUser.Firstname);
                }

                await provider2.DisposeAsync();
            }
            finally
            {
                if (System.IO.File.Exists(testDbPath))
                {
                    try { System.IO.File.Delete(testDbPath); } catch { }
                }
            }
        }

        [Fact]
        public async Task QoreIdService_ShouldVerifyBvnAndNinContinuously_InTestMode()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "QoreId:BaseUrl", "https://api.qoreid.com" },
                    { "QoreId:ClientId", "98CQY9HFI5A2JMXITE3U" },
                    { "QoreId:ClientSecret", "985c761048c34a63b1ebfad3284c1ca8" },
                    { "QoreId:IsTestMode", "true" }
                })
                .Build();

            var loggerMock = new Mock<ILogger<QoreIdService>>();
            var httpClient = new HttpClient();
            var qoreIdService = new QoreIdService(httpClient, config, loggerMock.Object);

            // 1. Verify BVN in test mode with any valid 11-digit BVN
            var bvnResult1 = await qoreIdService.VerifyBvnAsync("22234567890", "Amina", "Okafor");
            Assert.True(bvnResult1.Succeeded);

            // 2. Continuous testing with same BVN does not get flagged or rejected
            var bvnResult2 = await qoreIdService.VerifyBvnAsync("22234567890", "Amina", "Okafor");
            Assert.True(bvnResult2.Succeeded);

            // 3. Verify NIN in test mode
            var ninResult = await qoreIdService.VerifyNinAsync("10987654321", "Amina", "Okafor");
            Assert.True(ninResult.Succeeded);

            // 4. Invalid length should be rejected
            var invalidBvn = await qoreIdService.VerifyBvnAsync("123", "Amina", "Okafor");
            Assert.False(invalidBvn.Succeeded);
        }

        [Fact]
        public async Task CloudinaryService_EmptyBase64_ShouldReturnFailure()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Cloudinary:CloudName", "classicalaitem" },
                    { "Cloudinary:ApiKey", "898961726654369" },
                    { "Cloudinary:ApiSecret", "ttpGnA85PWRCystSkLavjyOKC7s" }
                })
                .Build();

            var loggerMock = new Mock<ILogger<CloudinaryService>>();
            var cloudinaryService = new CloudinaryService(config, loggerMock.Object);

            var result = await cloudinaryService.UploadBase64ImageAsync("", "nextbus/vehicles");
            Assert.False(result.Succeeded);
            Assert.Contains("cannot be empty", result.Errors[0]);
        }

        [Fact]
        public async Task UpdateRide_And_CheckIn_CheckOut_Workflow_Succeeds()
        {
            using var context = CreateInMemoryDbContext();
            var driver = new AppUser
            {
                Id = "driver-test",
                UserName = "driver@test.com",
                Email = "driver@test.com",
                Firstname = "Driver",
                Lastname = "One",
                KycStatus = KycStatus.Verified,
                DriverKycStatus = DriverKycStatus.Verified
            };

            var passenger = new AppUser
            {
                Id = "passenger-test",
                UserName = "passenger@test.com",
                Email = "passenger@test.com",
                Firstname = "Passenger",
                Lastname = "Two",
                KycStatus = KycStatus.Verified
            };

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                AppUserId = driver.Id,
                Make = "Honda",
                Model = "Civic",
                PlateNumber = "ABJ-909-CD",
                TotalSeats = 4,
                IsActive = true
            };

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                VehicleId = vehicle.Id,
                Origin = "Garki",
                Destination = "Wuse",
                DepartureTime = DateTime.UtcNow.AddHours(2),
                TotalSeatsOffered = 3,
                AvailableSeats = 3,
                PricePerSeat = 1000m,
                Status = RideStatus.Scheduled
            };

            await context.Users.AddRangeAsync(driver, passenger);
            await context.Vehicles.AddAsync(vehicle);
            await context.Rides.AddAsync(ride);
            await context.SaveChangesAsync();

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(c => c.UserId).Returns(driver.Id);
            var notificationServiceMock = new Mock<INotificationService>();

            // 1. Driver edits ride details (change destination to Maitama, fare to 1200)
            var updateHandler = new UpdateRideCommandHandler(
                context, 
                currentUserServiceMock.Object, 
                notificationServiceMock.Object, 
                _mapper);

            var updateResult = await updateHandler.Handle(new UpdateRideCommand(ride.Id, new UpdateRideDto
            {
                VehicleId = vehicle.Id,
                Origin = "Garki",
                Destination = "Maitama",
                DepartureTime = DateTime.UtcNow.AddHours(3),
                TotalSeatsOffered = 4,
                PricePerSeat = 1200m
            }), CancellationToken.None);

            Assert.True(updateResult.Succeeded);
            Assert.Equal("Maitama", updateResult.Data.Destination);
            Assert.Equal(1200m, updateResult.Data.PricePerSeat);

            // 2. Booking is created and accepted
            var booking = new RideBooking
            {
                Id = Guid.NewGuid(),
                RideId = ride.Id,
                PassengerId = passenger.Id,
                PickupLocation = "Garki",
                DropoffLocation = "Maitama",
                SeatsBooked = 1,
                PricePerSeat = 1200m,
                TotalFare = 1200m,
                BookingStatus = BookingStatus.Accepted,
                PaymentStatus = PaymentStatus.Paid
            };
            await context.RideBookings.AddAsync(booking);
            await context.SaveChangesAsync();

            // 3. Passenger checks in
            currentUserServiceMock.Setup(c => c.UserId).Returns(passenger.Id);
            var checkInHandler = new CheckInBookingCommandHandler(
                context, 
                currentUserServiceMock.Object, 
                notificationServiceMock.Object, 
                _mapper);

            var checkInResult = await checkInHandler.Handle(new CheckInBookingCommand(booking.Id), CancellationToken.None);
            Assert.True(checkInResult.Succeeded);
            Assert.Equal(BookingStatus.CheckedIn, checkInResult.Data.BookingStatus);
            Assert.NotNull(checkInResult.Data.CheckedInAt);

            // 4. Passenger checks out
            var checkOutHandler = new CheckOutBookingCommandHandler(
                context, 
                currentUserServiceMock.Object, 
                notificationServiceMock.Object, 
                _mapper);

            var checkOutResult = await checkOutHandler.Handle(new CheckOutBookingCommand(booking.Id), CancellationToken.None);
            Assert.True(checkOutResult.Succeeded);
            Assert.Equal(BookingStatus.CheckedOut, checkOutResult.Data.BookingStatus);
            Assert.NotNull(checkOutResult.Data.CheckedOutAt);
        }

        [Fact]
        public async Task CreateRideCommandValidator_WhenOnlyTotalSeatsOfferedProvided_PassesValidation()
        {
            var dto = new CreateRideDto
            {
                VehicleId = Guid.NewGuid(),
                Origin = "Ikorodu",
                Destination = "Ikeja",
                DepartureTime = DateTime.UtcNow.AddDays(1),
                TotalSeatsOffered = 4,
                PricePerSeat = 1500
            };

            var command = new CreateRideCommand(dto);
            var validator = new CreateRideCommandValidator();
            var result = await validator.ValidateAsync(command);

            Assert.Equal(4, command.Dto.AvailableSeats);
            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }
}
