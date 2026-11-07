using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Rides
{
    public record CreateRideCommand(CreateRideDto Dto) : IRequest<Result<RideDto>>;

    public class CreateRideCommandValidator : AbstractValidator<CreateRideCommand>
    {
        public CreateRideCommandValidator()
        {
            RuleFor(x => x.Dto.Origin).NotEmpty().WithMessage("Origin (starting location) is required.");
            RuleFor(x => x.Dto.Destination).NotEmpty().WithMessage("Destination is required.");
            RuleFor(x => x.Dto.AvailableSeats).GreaterThan(0).WithMessage("Available seats must be greater than 0.");
            RuleFor(x => x.Dto.PricePerSeat).GreaterThan(0).WithMessage("Fare per seat must be greater than 0.");
            RuleFor(x => x.Dto.DepartureTime).GreaterThan(DateTime.UtcNow).WithMessage("Departure time must be in the future.");
            RuleFor(x => x.Dto.VehicleId).NotEmpty().WithMessage("Vehicle is required.");
        }
    }

    public class CreateRideCommandHandler : IRequestHandler<CreateRideCommand, Result<RideDto>>
    {
        private readonly IAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public CreateRideCommandHandler(
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

        public async Task<Result<RideDto>> Handle(CreateRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RideDto>.Failure("Unauthorized. Please log in.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<RideDto>.Failure("User not found.");
            }

            // SAFETY & KYC ENFORCEMENT: Driver must have valid license verified
            if (user.DriverKycStatus != DriverKycStatus.Verified)
            {
                return Result<RideDto>.Failure(
                    "You cannot offer a ride without a verified driver's license. Please submit your driver's license for verification under Profile/KYC.");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(
                v => v.Id == request.Dto.VehicleId && v.AppUserId == userId && v.IsActive, cancellationToken);

            if (vehicle == null)
            {
                return Result<RideDto>.Failure("Vehicle not found or does not belong to you.");
            }

            if (request.Dto.AvailableSeats > vehicle.TotalSeats)
            {
                return Result<RideDto>.Failure(
                    $"Available seats ({request.Dto.AvailableSeats}) cannot exceed the vehicle's capacity ({vehicle.TotalSeats}).");
            }

            var ride = _mapper.Map<Ride>(request.Dto);
            ride.DriverId = userId;
            ride.TotalSeatsOffered = request.Dto.AvailableSeats;
            ride.AvailableSeats = request.Dto.AvailableSeats;
            ride.Status = RideStatus.Scheduled;

            await _context.Rides.AddAsync(ride, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Reload with relations for DTO mapping
            var createdRide = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstAsync(r => r.Id == ride.Id, cancellationToken);

            return Result<RideDto>.Success(_mapper.Map<RideDto>(createdRide), "Ride created successfully.");
        }
    }
}
