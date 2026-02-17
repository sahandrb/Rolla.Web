namespace Rolla.Application.Interfaces;

public interface INotificationService
{
    // این قرارداد رو اینجا بنویس تا همه جا شناخته بشه
    Task NotifyNewTripAsync(int tripId, double lat, double lng, decimal price);
    Task NotifyTripAcceptedAsync(int tripId, string riderId, string driverId);

    Task NotifyStatusChangeAsync(int tripId, string message);

    Task NotifyDriverAsync(string driverId, int tripId, double lat, double lng, decimal price);
}

