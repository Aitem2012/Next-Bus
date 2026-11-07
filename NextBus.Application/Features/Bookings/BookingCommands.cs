using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Extensions;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Bookings
{
    // 1. Request to Join / Book a Ride
    public record RequestRideBookingCommand(CreateBookingDto Dto) : IRequest<Result<RideBookingDto>>;

    public class RequestRideBookingCommandValidator : AbstractValidator<RequestRideBookingCommand>
    {
        public RequestRideBookingCommandValidator()
        {
            RuleFor(x => x.Dto.RideId).NotEmpty().WithMessage("Ride is required.");
            RuleFor(x => x.Dto.PickupLocation).NotEmpty().WithMessage("Pickup location is required.");
            RuleFor(x => x.Dto.DropoffLocation).NotEmpty().WithMessage("Drop-off location is required.");
            RuleFor(x => x.Dto.SeatsBooked).GreaterThan(0).WithMessage("Must book at least 1 seat.");
        }
    }

    public class RequestRideBookingCommandHandler : IRequestHandler<RequestRideBookingCommand, Result<RideBookingDto>>
    {
        private readonly IAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public RequestRideBookingCommandHandler(
            IAppDbContext context,
            UserManager<AppUser> userManager,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<RideBookingDto>> Handle(RequestRideBookingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RideBookingDto>.Failure("Unauthorized. Please log in.");
            }

            var passenger = await _userManager.FindByIdAsync(userId);
            if (passenger == null)
            {
                return Result<RideBookingDto>.Failure("User not found.");
            }

            // SAFETY & KYC ENFORCEMENT: Passenger must have verified KYC
            if (passenger.KycStatus != KycStatus.Verified)
            {
                return Result<RideBookingDto>.Failure(
                    "You must complete your identity verification (KYC with BVN/NIN) before requesting to join rides.");
            }

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.Id == request.Dto.RideId, cancellationToken);

            if (ride == null)
            {
                return Result<RideBookingDto>.Failure("Ride not found.");
            }

            if (ride.DriverId == userId)
            {
                return Result<RideBookingDto>.Failure("You cannot book your own ride.");
            }

            if (ride.Status != RideStatus.Scheduled)
            {
                return Result<RideBookingDto>.Failure("This ride is no longer accepting bookings.");
            }

            if (ride.AvailableSeats < request.Dto.SeatsBooked)
            {
                return Result<RideBookingDto>.Failure(
                    $"Not enough seats available. Only {ride.AvailableSeats} seat(s) remaining.");
            }

            var totalFare = ride.PricePerSeat * request.Dto.SeatsBooked;

            // Check passenger wallet balance
            var wallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);
            if (wallet == null || wallet.Balance < totalFare)
            {
                var balance = wallet?.Balance ?? 0m;
                return Result<RideBookingDto>.Failure(
                    $"Insufficient wallet balance. Total fare is ₦{totalFare:N2}, but your wallet balance is ₦{balance:N2}. Please top up your wallet.");
            }

            // Deduct fare to escrow pending driver approval
            wallet.Balance -= totalFare;
            wallet.LastUpdated = DateTime.UtcNow;

            var walletHistory = new WalletHistory
            {
                WalletId = wallet.Id,
                AppUserId = userId,
                Amount = totalFare,
                Type = "RidePayment",
                Reference = "RIDE".GenerateRef(),
                Details = $"Fare reserved for ride {ride.Origin} -> {ride.Destination} ({request.Dto.SeatsBooked} seat(s))",
                Date = DateTime.UtcNow
            };
            await _context.WalletHistories.AddAsync(walletHistory, cancellationToken);

            var booking = new RideBooking
            {
                RideId = ride.Id,
                PassengerId = userId,
                PickupLocation = request.Dto.PickupLocation,
                DropoffLocation = request.Dto.DropoffLocation,
                SeatsBooked = request.Dto.SeatsBooked,
                PricePerSeat = ride.PricePerSeat,
                TotalFare = totalFare,
                BookingStatus = BookingStatus.Requested,
                PaymentStatus = PaymentStatus.Paid,
                RequestedAt = DateTime.UtcNow,
                Notes = request.Dto.Notes
            };

            await _context.RideBookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var createdBooking = await _context.RideBookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .FirstAsync(b => b.Id == booking.Id, cancellationToken);

            return Result<RideBookingDto>.Success(
                _mapper.Map<RideBookingDto>(createdBooking),
                "Ride request submitted to the driver. The fare has been reserved from your wallet.");
        }
    }

    // 2. Respond to Booking (Driver Accept / Reject)
    public record RespondToBookingCommand(Guid BookingId, RespondBookingDto Dto) : IRequest<Result<RideBookingDto>>;

    public class RespondToBookingCommandHandler : IRequestHandler<RespondToBookingCommand, Result<RideBookingDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public RespondToBookingCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<RideBookingDto>> Handle(RespondToBookingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var booking = await _context.RideBookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null || booking.Ride == null)
            {
                return Result<RideBookingDto>.Failure("Booking not found.");
            }

            if (booking.Ride.DriverId != userId)
            {
                return Result<RideBookingDto>.Failure("Only the driver of this ride can respond to booking requests.");
            }

            if (booking.BookingStatus != BookingStatus.Requested)
            {
                return Result<RideBookingDto>.Failure($"Booking has already been {booking.BookingStatus}.");
            }

            booking.RespondedAt = DateTime.UtcNow;

            if (request.Dto.Accept)
            {
                if (booking.Ride.AvailableSeats < booking.SeatsBooked)
                {
                    return Result<RideBookingDto>.Failure("Not enough seats available to accept this booking.");
                }

                booking.BookingStatus = BookingStatus.Accepted;
                booking.Ride.AvailableSeats -= booking.SeatsBooked;
            }
            else
            {
                // Rejected: Refund the fare back to passenger wallet
                booking.BookingStatus = BookingStatus.Rejected;
                booking.PaymentStatus = PaymentStatus.Refunded;

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
                        Details = $"Refund for rejected booking request on ride {booking.Ride.Origin} -> {booking.Ride.Destination}",
                        Date = DateTime.UtcNow
                    };
                    await _context.WalletHistories.AddAsync(refundHistory, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<RideBookingDto>.Success(
                _mapper.Map<RideBookingDto>(booking),
                request.Dto.Accept ? "Booking accepted." : "Booking rejected and fare refunded to passenger.");
        }
    }

    // 3. Get My Bookings (Passenger)
    public record GetMyBookingsQuery : IRequest<Result<List<RideBookingDto>>>;

    public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, Result<List<RideBookingDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetMyBookingsQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<List<RideBookingDto>>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<List<RideBookingDto>>.Failure("Unauthorized.");
            }

            var bookings = await _context.RideBookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .Where(b => b.PassengerId == userId)
                .OrderByDescending(b => b.RequestedAt)
                .ToListAsync(cancellationToken);

            return Result<List<RideBookingDto>>.Success(_mapper.Map<List<RideBookingDto>>(bookings));
        }
    }

    // 4. Get Ride Bookings (Driver views requests for their ride)
    public record GetRideBookingsQuery(Guid RideId) : IRequest<Result<List<RideBookingDto>>>;

    public class GetRideBookingsQueryHandler : IRequestHandler<GetRideBookingsQuery, Result<List<RideBookingDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetRideBookingsQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<List<RideBookingDto>>> Handle(GetRideBookingsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);
            if (ride == null)
            {
                return Result<List<RideBookingDto>>.Failure("Ride not found.");
            }

            if (ride.DriverId != userId)
            {
                return Result<List<RideBookingDto>>.Failure("Only the driver can view booking requests for this ride.");
            }

            var bookings = await _context.RideBookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .Where(b => b.RideId == request.RideId)
                .OrderByDescending(b => b.RequestedAt)
                .ToListAsync(cancellationToken);

            return Result<List<RideBookingDto>>.Success(_mapper.Map<List<RideBookingDto>>(bookings));
        }
    }
}
