using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Domain.Entities;

namespace NextBus.Persistence.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Ride> Rides => Set<Ride>();
        public DbSet<RideBooking> RideBookings => Set<RideBooking>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<WalletHistory> WalletHistories => Set<WalletHistory>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User configuration
            builder.Entity<AppUser>(b =>
            {
                b.HasOne(u => u.Wallet)
                    .WithOne(w => w.AppUser)
                    .HasForeignKey<Wallet>(w => w.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(u => u.Vehicles)
                    .WithOne(v => v.AppUser)
                    .HasForeignKey(v => v.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(u => u.OfferedRides)
                    .WithOne(r => r.Driver)
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(u => u.Bookings)
                    .WithOne(b => b.Passenger)
                    .HasForeignKey(b => b.PassengerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Vehicle configuration
            builder.Entity<Vehicle>(b =>
            {
                b.HasKey(v => v.Id);
                b.HasIndex(v => v.PlateNumber).IsUnique();
            });

            // Ride configuration
            builder.Entity<Ride>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.PricePerSeat).HasPrecision(18, 2);

                b.HasOne(r => r.Vehicle)
                    .WithMany(v => v.Rides)
                    .HasForeignKey(r => r.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(r => r.Bookings)
                    .WithOne(b => b.Ride)
                    .HasForeignKey(b => b.RideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RideBooking configuration
            builder.Entity<RideBooking>(b =>
            {
                b.HasKey(rb => rb.Id);
                b.Property(rb => rb.PricePerSeat).HasPrecision(18, 2);
                b.Property(rb => rb.TotalFare).HasPrecision(18, 2);
                b.Property(rb => rb.CommissionAmount).HasPrecision(18, 2);
                b.Property(rb => rb.DriverEarnings).HasPrecision(18, 2);
            });

            // Wallet configuration
            builder.Entity<Wallet>(b =>
            {
                b.HasKey(w => w.Id);
                b.Property(w => w.Balance).HasPrecision(18, 2);

                b.HasMany(w => w.Histories)
                    .WithOne(h => h.Wallet)
                    .HasForeignKey(h => h.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WalletHistory configuration
            builder.Entity<WalletHistory>(b =>
            {
                b.HasKey(wh => wh.Id);
                b.Property(wh => wh.Amount).HasPrecision(18, 2);
            });

            // Transaction configuration
            builder.Entity<Transaction>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Amount).HasPrecision(18, 2);

                b.HasOne(t => t.AppUser)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(t => t.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OtpVerification configuration
            builder.Entity<OtpVerification>(b =>
            {
                b.HasKey(o => o.Id);
                b.HasIndex(o => new { o.Email, o.Purpose });
            });

            // Notification configuration
            builder.Entity<Notification>(b =>
            {
                b.HasKey(n => n.Id);
                b.HasIndex(n => n.AppUserId);
                b.HasIndex(n => n.IsRead);
            });
        }
    }
}
