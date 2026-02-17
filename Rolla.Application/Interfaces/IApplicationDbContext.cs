using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rolla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Rolla.Domain.Entities;



namespace Rolla.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Trip> Trips { get; set; }

    // ✨ این دو خط را اضافه کنید:
    DbSet<ApplicationUser> Users { get; }
    DbSet<WalletTransaction> WalletTransactions { get; set; }
    DbSet<TripRequestLog> TripRequestLogs { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}