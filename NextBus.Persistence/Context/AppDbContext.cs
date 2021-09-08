using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Persistence.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>, IAppDbContext
    {
        public AppDbContext()
        {
            
        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public static AppDbContext Create()
        {
            return new AppDbContext();
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletHistory> WalletHistories { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Driver>(x => x.HasKey(a => a.AppUserId));
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }
    }
}
