using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Rides
{
    public record SearchRidesQuery(RideSearchQueryDto Criteria) : IRequest<Result<List<RideDto>>>;

    public class SearchRidesQueryHandler : IRequestHandler<SearchRidesQuery, Result<List<RideDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public SearchRidesQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<List<RideDto>>> Handle(SearchRidesQuery request, CancellationToken cancellationToken)
        {
            var criteria = request.Criteria;
            var query = _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Where(r => r.Status == RideStatus.Scheduled && r.AvailableSeats > 0 && r.DepartureTime >= DateTime.UtcNow);

            if (criteria.RequiredSeats.HasValue && criteria.RequiredSeats > 0)
            {
                query = query.Where(r => r.AvailableSeats >= criteria.RequiredSeats.Value);
            }

            if (criteria.IsPublicTransit.HasValue)
            {
                query = query.Where(r => r.IsPublicTransit == criteria.IsPublicTransit.Value);
            }

            if (criteria.Date.HasValue)
            {
                var targetDate = criteria.Date.Value.Date;
                var nextDate = targetDate.AddDays(1);
                query = query.Where(r => r.DepartureTime >= targetDate && r.DepartureTime < nextDate);
            }

            var rides = await query
                .OrderBy(r => r.DepartureTime)
                .ToListAsync(cancellationToken);

            // Filter routes: Origin & Destination matching start, stops, or end
            var originFilter = criteria.Origin?.Trim().ToLowerInvariant();
            var destFilter = criteria.Destination?.Trim().ToLowerInvariant();

            var filtered = rides.Where(r =>
            {
                bool matchesOrigin = true;
                bool matchesDestination = true;

                if (!string.IsNullOrEmpty(originFilter))
                {
                    matchesOrigin = r.Origin.ToLower().Contains(originFilter) ||
                                   (r.Stop1 != null && r.Stop1.ToLower().Contains(originFilter));
                }

                if (!string.IsNullOrEmpty(destFilter))
                {
                    matchesDestination = r.Destination.ToLower().Contains(destFilter) ||
                                         (r.Stop1 != null && r.Stop1.ToLower().Contains(destFilter)) ||
                                         (r.Stop2 != null && r.Stop2.ToLower().Contains(destFilter));
                }

                return matchesOrigin && matchesDestination;
            }).ToList();

            return Result<List<RideDto>>.Success(_mapper.Map<List<RideDto>>(filtered));
        }
    }
}
