using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Rolla.Domain.Entities;
using Rolla.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Rolla.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Trip> Trips { get; set; }
    DatabaseFacade Database { get; }
    // ✨ این دو خط را اضافه کنید:
    DbSet<ApplicationUser> Users { get; }
    DbSet<WalletTransaction> WalletTransactions { get; set; }
    DbSet<TripRequestLog> TripRequestLogs { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<DriverDocument> DriverDocuments { get; set; }
    DbSet<ChatMessage> ChatMessages { get; set; } // نام را جمع (Messages) بگذارید
}