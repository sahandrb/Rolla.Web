using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Enums;

namespace Rolla.Web.Services;

public class TripDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TripDispatcherService> _logger;

    public TripDispatcherService(IServiceProvider serviceProvider, ILogger<TripDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // ✅ دریافت سرویس
                    var tripService = scope.ServiceProvider.GetRequiredService<ITripService>();

                    // ✅ فقط فراخوانی متد پردازش (بدون هیچ شرط و شروطی در اینجا)
                    await tripService.ProcessPendingTripsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TripDispatcher");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}