using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Extensions;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Rides
{
    // 1. Get Ride By Id
    public record GetRideByIdQuery(Guid Id) : IRequest<Result<RideDto>>;

    public class GetRideByIdQueryHandler : IRequestHandler<GetRideByIdQuery, Result<RideDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetRideByIdQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<RideDto>> Handle(GetRideByIdQuery request, CancellationToken cancellationToken)
        {
            var ride = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (ride == null)
            {
                return Result<RideDto>.Failure("Ride not found.");
            }

            return Result<RideDto>.Success(_mapper.Map<RideDto>(ride));
        }
    }

    // 2. Get My Offered Rides (Driver)
    public record GetMyOfferedRidesQuery : IRequest<Result<List<RideDto>>>;

    public class GetMyOfferedRidesQueryHandler : IRequestHandler<GetMyOfferedRidesQuery, Result<List<RideDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetMyOfferedRidesQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<List<RideDto>>> Handle(GetMyOfferedRidesQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<List<RideDto>>.Failure("Unauthorized.");
            }

            var rides = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Where(r => r.DriverId == userId)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync(cancellationToken);

            return Result<List<RideDto>>.Success(_mapper.Map<List<RideDto>>(rides));
        }
    }

    // 3. Start Ride Command (Driver sets InProgress)
    public record StartRideCommand(Guid RideId) : IRequest<Result>;

    public class StartRideCommandHandler : IRequestHandler<StartRideCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StartRideCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(StartRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId && r.DriverId == userId, cancellationToken);

            if (ride == null)
            {
                return Result.Failure("Ride not found or you are not the driver.");
            }

            if (ride.Status != RideStatus.Scheduled)
            {
                return Result.Failure($"Ride cannot be started because it is currently {ride.Status}.");
            }

            ride.Status = RideStatus.InProgress;
            ride.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success("Ride is now in progress.");
        }
    }

    // 4. Complete Ride Command (Transfers earnings net of commission to driver's wallet)
    public record CompleteRideCommand(Guid RideId) : IRequest<Result>;

    public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public CompleteRideCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            INotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task<Result> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var ride = await _context.Rides
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Passenger)
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.Id == request.RideId && r.DriverId == userId, cancellationToken);

            if (ride == null)
            {
                return Result.Failure("Ride not found or you are not the driver.");
            }

            if (ride.Status == RideStatus.Completed)
            {
                return Result.Failure("Ride has already been completed.");
            }

            ride.Status = RideStatus.Completed;
            ride.UpdatedAt = DateTime.UtcNow;

            var commissionRate = decimal.TryParse(_configuration["PlatformSettings:CommissionPercentage"], out var cp) ? cp : 10.0m;

            // Pay for all accepted, checked-in, or checked-out passenger bookings
            var payableBookings = ride.Bookings.Where(b => 
                (b.BookingStatus == BookingStatus.Accepted || 
                 b.BookingStatus == BookingStatus.CheckedIn || 
                 b.BookingStatus == BookingStatus.CheckedOut) &&
                b.PaymentStatus == PaymentStatus.Paid).ToList();

            decimal totalDriverEarnings = 0m;
            decimal totalCommission = 0m;

            foreach (var booking in payableBookings)
            {
                var commission = Math.Round(booking.TotalFare * (commissionRate / 100m), 2);
                var driverEarnings = booking.TotalFare - commission;

                booking.CommissionAmount = commission;
                booking.DriverEarnings = driverEarnings;
                booking.BookingStatus = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;

                totalDriverEarnings += driverEarnings;
                totalCommission += commission;

                var commTx = new Transaction
                {
                    AppUserId = userId!,
                    Amount = commission,
                    Type = TransactionType.PlatformCommission,
                    Reference = "COMM".GenerateRef(),
                    Description = $"Platform {commissionRate}% commission on passenger booking {booking.Id} for ride {ride.Origin} -> {ride.Destination}",
                    IsSuccessful = true,
                    Date = DateTime.UtcNow
                };
                await _context.Transactions.AddAsync(commTx, cancellationToken);

                // Notify passenger that the journey is marked complete
                await _notificationService.CreateNotificationAsync(
                    booking.PassengerId,
                    "🎉 Commute Trip Completed",
                    $"Your ride from {booking.PickupLocation} to {booking.DropoffLocation} has been marked complete. Thank you for commuting with NextBus!",
                    "Ride",
                    "/my-bookings");
            }

            if (totalDriverEarnings > 0)
            {
                var driverWallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);
                if (driverWallet != null)
                {
                    driverWallet.Balance += totalDriverEarnings;
                    driverWallet.LastUpdated = DateTime.UtcNow;

                    var history = new WalletHistory
                    {
                        WalletId = driverWallet.Id,
                        AppUserId = userId!,
                        Amount = totalDriverEarnings,
                        Type = "RideEarnings",
                        Reference = "EARN".GenerateRef(),
                        Details = $"Earnings (net of {commissionRate}% platform commission) for completed ride {ride.Origin} -> {ride.Destination} ({payableBookings.Count} passenger(s))",
                        Date = DateTime.UtcNow
                    };
                    await _context.WalletHistories.AddAsync(history, cancellationToken);

                    var earnTx = new Transaction
                    {
                        AppUserId = userId!,
                        Amount = totalDriverEarnings,
                        Type = TransactionType.RideEarnings,
                        Reference = history.Reference,
                        Description = history.Details,
                        IsSuccessful = true,
                        Date = DateTime.UtcNow
                    };
                    await _context.Transactions.AddAsync(earnTx, cancellationToken);

                    // Notify driver that funds were credited
                    await _notificationService.CreateNotificationAsync(
                        userId!,
                        $"💰 Commute Earnings Credited: ₦{totalDriverEarnings:N2}",
                        $"You earned ₦{totalDriverEarnings:N2} (net of ₦{totalCommission:N2} platform fee) for completing your ride {ride.Origin} → {ride.Destination}.",
                        "Wallet",
                        "/wallet");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success($"Ride completed. Total earnings of ₦{totalDriverEarnings:N2} (net of ₦{totalCommission:N2} platform commission) credited to your wallet.");
        }
    }

    // 5. Cancel Ride Command (Driver cancels and auto-refunds all passengers)
    public record CancelRideCommand(Guid RideId) : IRequest<Result>;

    public class CancelRideCommandHandler : IRequestHandler<CancelRideCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public CancelRideCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<Result> Handle(CancelRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var ride = await _context.Rides
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.Id == request.RideId && r.DriverId == userId, cancellationToken);

            if (ride == null)
            {
                return Result.Failure("Ride not found or you are not the driver.");
            }

            if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
            {
                return Result.Failure($"Ride cannot be cancelled because it is already {ride.Status}.");
            }

            ride.Status = RideStatus.Cancelled;
            ride.UpdatedAt = DateTime.UtcNow;

            // Automatically refund all passengers who have paid bookings
            var activeBookings = ride.Bookings
                .Where(b => (b.BookingStatus == BookingStatus.Accepted || b.BookingStatus == BookingStatus.Requested || b.BookingStatus == BookingStatus.CheckedIn) && b.PaymentStatus == PaymentStatus.Paid)
                .ToList();

            foreach (var booking in activeBookings)
            {
                if (booking.PaymentStatus == PaymentStatus.Paid)
                {
                    var passengerWallet = await _context.Wallets
                        .SingleOrDefaultAsync(w => w.AppUserId == booking.PassengerId, cancellationToken);

                    if (passengerWallet != null)
                    {
                        passengerWallet.Balance += booking.TotalFare;
                        passengerWallet.LastUpdated = DateTime.UtcNow;

                        var refundHistory = new WalletHistory
                        {
                            WalletId = passengerWallet.Id,
                            AppUserId = booking.PassengerId,
                            Amount = booking.TotalFare,
                            Type = "Refund",
                            Reference = "RFND".GenerateRef(),
                            Details = $"Refund due to driver cancellation of ride {ride.Origin} -> {ride.Destination}",
                            Date = DateTime.UtcNow
                        };
                        await _context.WalletHistories.AddAsync(refundHistory, cancellationToken);
                    }
                    booking.PaymentStatus = PaymentStatus.Refunded;
                }

                booking.BookingStatus = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var booking in activeBookings)
            {
                await _notificationService.CreateNotificationAsync(
                    booking.PassengerId,
                    "⚠️ Ride Cancelled by Driver",
                    $"The driver cancelled the ride {ride.Origin} → {ride.Destination}. A full refund of ₦{booking.TotalFare:N2} has been returned to your wallet.",
                    "Ride",
                    "/my-bookings");
            }

            return Result.Success("Ride cancelled and all passenger fares have been refunded.");
        }
    }
}
