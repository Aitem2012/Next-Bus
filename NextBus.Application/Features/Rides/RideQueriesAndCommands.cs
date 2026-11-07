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

    // 3. Complete Ride Command (Transfers earnings to driver's wallet)
    public record CompleteRideCommand(Guid RideId) : IRequest<Result>;

    public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CompleteRideCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var ride = await _context.Rides
                .Include(r => r.Bookings)
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

            // Credit driver wallet with total paid fares from accepted bookings
            var acceptedBookings = ride.Bookings.Where(b => b.BookingStatus == BookingStatus.Accepted).ToList();
            var totalEarnings = acceptedBookings.Sum(b => b.TotalFare);

            if (totalEarnings > 0)
            {
                var driverWallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);
                if (driverWallet != null)
                {
                    driverWallet.Balance += totalEarnings;
                    driverWallet.LastUpdated = DateTime.UtcNow;

                    var history = new WalletHistory
                    {
                        WalletId = driverWallet.Id,
                        AppUserId = userId!,
                        Amount = totalEarnings,
                        Type = "RideEarnings",
                        Reference = "EARN".GenerateRef(),
                        Details = $"Earnings for completed ride {ride.Origin} -> {ride.Destination}",
                        Date = DateTime.UtcNow
                    };
                    await _context.WalletHistories.AddAsync(history, cancellationToken);
                }
            }

            foreach (var booking in acceptedBookings)
            {
                booking.BookingStatus = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success("Ride completed and earnings credited to your wallet.");
        }
    }
}
