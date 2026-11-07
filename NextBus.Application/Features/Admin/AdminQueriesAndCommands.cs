using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Admin
{
    // 1. Get Admin Overview KPI Stats
    public record GetAdminOverviewQuery : IRequest<Result<AdminOverviewDto>>;

    public class GetAdminOverviewQueryHandler : IRequestHandler<GetAdminOverviewQuery, Result<AdminOverviewDto>>
    {
        private readonly IAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public GetAdminOverviewQueryHandler(IAppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result<AdminOverviewDto>> Handle(GetAdminOverviewQuery request, CancellationToken cancellationToken)
        {
            var totalUsers = await _userManager.Users.CountAsync(cancellationToken);
            var totalDrivers = await _userManager.Users.CountAsync(u => u.DriverKycStatus == DriverKycStatus.Verified, cancellationToken);
            var totalPassengers = totalUsers - totalDrivers;

            var totalVehicles = await _context.Vehicles.CountAsync(cancellationToken);
            var pendingVehicles = await _context.Vehicles.CountAsync(v => !v.IsApprovedByAdmin, cancellationToken);

            var totalRides = await _context.Rides.CountAsync(cancellationToken);
            var activeRides = await _context.Rides.CountAsync(r => r.Status == RideStatus.Scheduled || r.Status == RideStatus.InProgress, cancellationToken);

            var totalBookings = await _context.RideBookings.CountAsync(cancellationToken);

            // Escrow funds currently reserved for requested/accepted rides
            var escrowStatuses = new[] { BookingStatus.Requested, BookingStatus.Accepted };
            var totalEscrow = await _context.RideBookings
                .Where(b => escrowStatuses.Contains(b.BookingStatus))
                .SumAsync(b => (decimal?)b.TotalFare, cancellationToken) ?? 0m;

            // Total platform commissions earned from completed rides
            var totalCommission = await _context.RideBookings
                .Where(b => b.BookingStatus == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.CommissionAmount, cancellationToken) ?? 0m;

            var dto = new AdminOverviewDto
            {
                TotalUsers = totalUsers,
                TotalDrivers = totalDrivers,
                TotalPassengers = totalPassengers,
                TotalVehicles = totalVehicles,
                PendingVehicles = pendingVehicles,
                TotalRides = totalRides,
                ActiveRides = activeRides,
                TotalBookings = totalBookings,
                TotalEscrowHeld = totalEscrow,
                TotalPlatformCommission = totalCommission
            };

            return Result<AdminOverviewDto>.Success(dto);
        }
    }

    // 2. Get All Users
    public record GetAdminUsersQuery : IRequest<Result<List<UserDto>>>;

    public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, Result<List<UserDto>>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public GetAdminUsersQueryHandler(UserManager<AppUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<Result<List<UserDto>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userManager.Users
                .Include(u => u.Wallet)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var dto = _mapper.Map<UserDto>(user);
                dto.Roles = roles;
                dto.IsAdmin = roles.Contains("Admin");
                dto.WalletBalance = user.Wallet?.Balance ?? 0m;
                result.Add(dto);
            }

            return Result<List<UserDto>>.Success(result);
        }
    }

    // 3. Get All Vehicles for Admin Review (with 4-Corner Photos and OCR data)
    public record GetAdminVehiclesQuery : IRequest<Result<List<VehicleDto>>>;

    public class GetAdminVehiclesQueryHandler : IRequestHandler<GetAdminVehiclesQuery, Result<List<VehicleDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetAdminVehiclesQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<List<VehicleDto>>> Handle(GetAdminVehiclesQuery request, CancellationToken cancellationToken)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.AppUser)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = vehicles.Select(v =>
            {
                var d = _mapper.Map<VehicleDto>(v);
                d.DriverName = $"{v.AppUser?.Firstname} {v.AppUser?.Lastname}".Trim();
                return d;
            }).ToList();

            return Result<List<VehicleDto>>.Success(dtos);
        }
    }

    // 4. Approve Vehicle
    public record ApproveVehicleCommand(Guid VehicleId) : IRequest<Result<bool>>;

    public class ApproveVehicleCommandHandler : IRequestHandler<ApproveVehicleCommand, Result<bool>>
    {
        private readonly IAppDbContext _context;

        public ApproveVehicleCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(ApproveVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles.FindAsync(new object[] { request.VehicleId }, cancellationToken);
            if (vehicle == null)
            {
                return Result<bool>.Failure("Vehicle not found.");
            }

            vehicle.IsApprovedByAdmin = true;
            vehicle.IsActive = true;
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true, "Vehicle approved successfully.");
        }
    }

    // 5. Approve Driver KYC
    public record ApproveDriverKycCommand(string UserId) : IRequest<Result<bool>>;

    public class ApproveDriverKycCommandHandler : IRequestHandler<ApproveDriverKycCommand, Result<bool>>
    {
        private readonly UserManager<AppUser> _userManager;

        public ApproveDriverKycCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<bool>> Handle(ApproveDriverKycCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<bool>.Failure("User not found.");
            }

            user.DriverKycStatus = DriverKycStatus.Verified;
            user.DriverVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Driver"))
            {
                await _userManager.AddToRoleAsync(user, "Driver");
            }

            await _userManager.UpdateAsync(user);
            return Result<bool>.Success(true, "Driver license and accreditation approved by administrator.");
        }
    }
}
