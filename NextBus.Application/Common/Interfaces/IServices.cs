using System.Collections.Generic;
using NextBus.Domain.Entities;

namespace NextBus.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
    }

    public interface IJwtTokenService
    {
        string GenerateToken(AppUser user, IList<string> roles);
    }
}
