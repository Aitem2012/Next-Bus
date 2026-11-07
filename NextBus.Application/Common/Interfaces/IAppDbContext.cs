using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextBus.Domain.Entities;

namespace NextBus.Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<AppUser> Users { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<Ride> Rides { get; }
        DbSet<RideBooking> RideBookings { get; }
        DbSet<Wallet> Wallets { get; }
        DbSet<WalletHistory> WalletHistories { get; }
        DbSet<Transaction> Transactions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
