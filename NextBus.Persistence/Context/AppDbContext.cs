using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain.Buses;
using NextBus.Domain.Drivers;
using NextBus.Domain.Users;
using NextBus.Domain.Wallets;
using System.Threading;
using System.Threading.Tasks;
using NextBus.Domain.Transactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        public class IdentityUserLoginEntityTypesConfiguration : IEntityTypeConfiguration<IdentityUserLogin<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
            {
                builder.ToTable("AspNetUserLogin");
                builder.HasKey(i => i.UserId);
            }
        }

        public class IdentityUserRoleEntityTypesConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
            {
                builder.ToTable("AspNetUserRole");
                builder.HasKey(i => new { i.RoleId, i.UserId });
            }
        }

        public class IdentityRoleEntityTypesConfiguration : IEntityTypeConfiguration<IdentityRole<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityRole<string>> builder)
            {
                builder.ToTable("AspNetRole");
                builder.HasKey(i => i.Id);
            }
        }

        public class IdentityRoleClaimEntityTypesConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> builder)
            {
                builder.ToTable("AspNetRoleClaim");
                builder.HasKey(i => i.Id);
            }
        }

        public class IdentityUserClaimEntityTypesConfiguration : IEntityTypeConfiguration<IdentityUserClaim<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
            {
                builder.ToTable("AspNetUserClaim");
                builder.HasKey(i => i.Id);
            }
        }

        public class IdentityUserTokenEntityTypesConfiguration : IEntityTypeConfiguration<IdentityUserToken<string>>
        {
            public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
            {
                builder.ToTable("AspNetUserToken");
                builder.HasKey(i => i.UserId);
            }
        }
    }
}
