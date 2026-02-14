using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rolla.Domain.Entities;


namespace Rolla.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Trip> Trips { get; set; } // جدول سفرها

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Trip>().Property(x => x.Price).HasPrecision(18, 2);
            // تنظیم دیتابیس برای ذخیره مختصات به صورت جغرافیایی
            builder.Entity<Trip>(entity =>
            {
                entity.Property(e => e.Origin).HasColumnType("geography");
                entity.Property(e => e.Destination).HasColumnType("geography");
            });
        }
    }

}

