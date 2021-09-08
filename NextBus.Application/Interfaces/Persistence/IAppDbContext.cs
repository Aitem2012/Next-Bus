using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextBus.Domain;

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
