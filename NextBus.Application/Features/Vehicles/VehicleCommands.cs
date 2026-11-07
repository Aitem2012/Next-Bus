using System;
using System.Collections.Generic;
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
using NextBus.Domain.Entities;

namespace NextBus.Application.Features.Vehicles
{
    // 1. Register Vehicle
    public record RegisterVehicleCommand(CreateVehicleDto Dto) : IRequest<Result<VehicleDto>>;

    public class RegisterVehicleCommandValidator : AbstractValidator<RegisterVehicleCommand>
    {
        public RegisterVehicleCommandValidator()
        {
            RuleFor(x => x.Dto.Make).NotEmpty().WithMessage("Vehicle make is required.");
            RuleFor(x => x.Dto.Model).NotEmpty().WithMessage("Vehicle model is required.");
            RuleFor(x => x.Dto.Year).InclusiveBetween(1990, DateTime.UtcNow.Year + 1).WithMessage("Valid vehicle year is required.");
            RuleFor(x => x.Dto.PlateNumber).NotEmpty().WithMessage("Plate number is required.");
            RuleFor(x => x.Dto.TotalSeats).GreaterThan(0).WithMessage("Vehicle must have at least 1 passenger seat.");
        }
    }

    public class RegisterVehicleCommandHandler : IRequestHandler<RegisterVehicleCommand, Result<VehicleDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public RegisterVehicleCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<VehicleDto>> Handle(RegisterVehicleCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<VehicleDto>.Failure("Unauthorized.");
            }

            var plateNumber = request.Dto.PlateNumber.Trim().ToUpperInvariant();
            var exists = await _context.Vehicles.AnyAsync(v => v.PlateNumber == plateNumber, cancellationToken);
            if (exists)
            {
                return Result<VehicleDto>.Failure($"A vehicle with plate number '{plateNumber}' is already registered.");
            }

            var vehicle = _mapper.Map<Vehicle>(request.Dto);
            vehicle.AppUserId = userId;
            vehicle.PlateNumber = plateNumber;
            vehicle.IsActive = true;

            await _context.Vehicles.AddAsync(vehicle, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<VehicleDto>.Success(_mapper.Map<VehicleDto>(vehicle), "Vehicle registered successfully.");
        }
    }

    // 2. Get My Vehicles Query
    public record GetMyVehiclesQuery : IRequest<Result<List<VehicleDto>>>;

    public class GetMyVehiclesQueryHandler : IRequestHandler<GetMyVehiclesQuery, Result<List<VehicleDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetMyVehiclesQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<List<VehicleDto>>> Handle(GetMyVehiclesQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<List<VehicleDto>>.Failure("Unauthorized.");
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.AppUserId == userId && v.IsActive)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<VehicleDto>>.Success(_mapper.Map<List<VehicleDto>>(vehicles));
        }
    }
}
