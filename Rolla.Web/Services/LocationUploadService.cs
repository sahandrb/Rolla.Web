namespace Rolla.Web.Services;

// این کلاس یک "BackgroundService" است، یعنی به محض روشن شدن سرور،
// در یک رشته (Thread) جداگانه شروع به کار می‌کند و تا ابد بیدار می‌ماند.
public class LocationUploadService : BackgroundService
{
    private readonly LocationAggregator _aggregator;
    private readonly ILogger<LocationUploadService> _logger;

    public LocationUploadService(LocationAggregator aggregator, ILogger<LocationUploadService> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 موتور آپلود دسته‌ای لوکیشن روشن شد.");

        // تا زمانی که سرور روشن است (StoppingToken لغو نشده)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ۱. دو ثانیه صبر کن (Aggregation Period)
                // این همان زمانی است که لوکیشن‌ها در بافر جمع می‌شوند
                await Task.Delay(2000, stoppingToken);

                // ۲. حالا هر چی تو بافر جمع شده رو شلیک کن سمت Redis
                await _aggregator.FlushToRedisAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطا در حین تخلیه بافر به ردیس");
            }
        }
    }
}