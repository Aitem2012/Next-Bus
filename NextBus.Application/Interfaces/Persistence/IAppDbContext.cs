using Microsoft.EntityFrameworkCore;
using NextBus.Domain.Buses;
using NextBus.Domain.Drivers;
using NextBus.Domain.Transactions;
using NextBus.Domain.Users;
using NextBus.Domain.Wallets;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Application.Interfaces.Persistence
{
    public interface IAppDbContext 
    {
        
        DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        DbSet<Transaction> Transactions { get; set; }
        DbSet<Wallet> Wallets { get; set; }
        DbSet<WalletHistory> WalletHistories { get; set; }


        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
