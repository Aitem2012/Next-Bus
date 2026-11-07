using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Rides
{
    public record UpdateRideCommand(Guid RideId, UpdateRideDto Dto) : IRequest<Result<RideDto>>
    {
        public UpdateRideDto Dto { get; init; } = Normalize(Dto);

        private static UpdateRideDto Normalize(UpdateRideDto dto)
        {
            if (dto != null)
            {
                if (dto.AvailableSeats <= 0 && dto.TotalSeatsOffered > 0)
                {
                    dto.AvailableSeats = dto.TotalSeatsOffered;
                }
                else if (dto.TotalSeatsOffered <= 0 && dto.AvailableSeats > 0)
                {
                    dto.TotalSeatsOffered = dto.AvailableSeats;
                }
            }
            return dto!;
        }
    }

    public class UpdateRideCommandValidator : AbstractValidator<UpdateRideCommand>
    {
        public UpdateRideCommandValidator()
        {
            RuleFor(x => x.RideId).NotEmpty().WithMessage("Ride ID is required.");
            RuleFor(x => x.Dto.Origin).NotEmpty().WithMessage("Origin (starting location) is required.");
            RuleFor(x => x.Dto.Destination).NotEmpty().WithMessage("Destination is required.");
            RuleFor(x => x.Dto.TotalSeatsOffered).GreaterThan(0).WithMessage("Total seats must be greater than 0.");
            RuleFor(x => x.Dto.PricePerSeat).GreaterThan(0).WithMessage("Fare per seat must be greater than 0.");
            RuleFor(x => x.Dto.VehicleId).NotEmpty().WithMessage("Vehicle is required.");
        }
    }

    public class UpdateRideCommandHandler : IRequestHandler<UpdateRideCommand, Result<RideDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public UpdateRideCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<Result<RideDto>> Handle(UpdateRideCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RideDto>.Failure("Unauthorized. Please log in.");
            }

            var ride = await _context.Rides
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Passenger)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == request.RideId && r.DriverId == userId, cancellationToken);

            if (ride == null)
            {
                return Result<RideDto>.Failure("Ride not found or you are not the driver.");
            }

            if (ride.Status != RideStatus.Scheduled)
            {
                return Result<RideDto>.Failure($"This ride cannot be edited because it is already {ride.Status}.");
            }

            // Verify vehicle
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(
                v => v.Id == request.Dto.VehicleId && v.AppUserId == userId && v.IsActive, cancellationToken);

            if (vehicle == null)
            {
                return Result<RideDto>.Failure("Vehicle not found or does not belong to you.");
            }

            if (request.Dto.TotalSeatsOffered > vehicle.TotalSeats)
            {
                return Result<RideDto>.Failure(
                    $"Total seats offered ({request.Dto.TotalSeatsOffered}) cannot exceed vehicle capacity ({vehicle.TotalSeats}).");
            }

            // Calculate currently confirmed passenger seats
            var confirmedSeats = ride.Bookings
                .Where(b => b.BookingStatus == BookingStatus.Accepted || b.BookingStatus == BookingStatus.CheckedIn)
                .Sum(b => b.SeatsBooked);

            if (request.Dto.TotalSeatsOffered < confirmedSeats)
            {
                return Result<RideDto>.Failure(
                    $"Total seats cannot be reduced below the {confirmedSeats} seats already booked by passengers.");
            }

            var newAvailableSeats = request.Dto.TotalSeatsOffered - confirmedSeats;

            ride.Origin = request.Dto.Origin;
            ride.Destination = request.Dto.Destination;
            ride.Stop1 = request.Dto.Stop1;
            ride.Stop2 = request.Dto.Stop2;
            ride.DepartureTime = DateTime.SpecifyKind(request.Dto.DepartureTime, DateTimeKind.Utc);
            ride.TotalSeatsOffered = request.Dto.TotalSeatsOffered;
            ride.AvailableSeats = newAvailableSeats;
            ride.PricePerSeat = request.Dto.PricePerSeat;
            ride.VehicleId = request.Dto.VehicleId;
            ride.IsPublicTransit = request.Dto.IsPublicTransit;
            ride.Notes = request.Dto.Notes;
            ride.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Notify all confirmed passengers of the schedule / route update
            var activePassengers = ride.Bookings
                .Where(b => b.BookingStatus == BookingStatus.Accepted || b.BookingStatus == BookingStatus.CheckedIn)
                .Select(b => b.PassengerId)
                .Distinct()
                .ToList();

            foreach (var passengerId in activePassengers)
            {
                await _notificationService.CreateNotificationAsync(
                    passengerId,
                    "ℹ️ Ride Details Updated",
                    $"Driver updated trip details for {ride.Origin} → {ride.Destination}. Departure is set for {ride.DepartureTime:ddd, MMM d, h:mm tt UTC}.",
                    "Ride",
                    "/my-bookings");
            }

            // Reload ride with driver and vehicle for mapping
            var updatedRide = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstAsync(r => r.Id == ride.Id, cancellationToken);

            return Result<RideDto>.Success(_mapper.Map<RideDto>(updatedRide), "Ride details updated successfully.");
        }
    }
}
