using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;


namespace Rolla.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Trip> Trips { get; set; } // جدول سفرها
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // تنظیمات قبلی تریپ (دست نزنید)
            builder.Entity<Trip>().Property(x => x.Price).HasPrecision(18, 2);
            builder.Entity<Trip>(entity =>
            {
                entity.Property(e => e.Origin).HasColumnType("geography");
                entity.Property(e => e.Destination).HasColumnType("geography");
            });

            // ✨ تنظیمات جدید برای سیستم مالی
            // 1. تنظیم دقت موجودی کاربر (مثلاً تا 2 رقم اعشار)
            builder.Entity<ApplicationUser>()
                   .Property(u => u.WalletBalance)
                   .HasColumnType("decimal(18,2)"); // یا HasPrecision(18, 2)

            // 2. تنظیم دقت مبلغ تراکنش
            builder.Entity<WalletTransaction>()
                   .Property(t => t.Amount)
                   .HasColumnType("decimal(18,2)");


            builder.Entity<ApplicationUser>()
       .Property(u => u.WalletBalance)
       .HasDefaultValue(0m); // موجودی اولیه همه 0 باشد
        }
    }

}

