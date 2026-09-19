using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Development
{
    public sealed class RecordingNotificationService : INotificationService
    {
        private readonly INotificationService _inner;
        private readonly NotificationRecorder _recorder;
        private readonly ILogger<RecordingNotificationService> _logger;

        public RecordingNotificationService(
            INotificationService inner,
            NotificationRecorder recorder,
            ILogger<RecordingNotificationService> logger)
        {
            _inner = inner;
            _recorder = recorder;
            _logger = logger;
        }

        public void SendDelayNotification(int leadTime, string productName)
        {
            Capture($"DELAY       | {productName} | back in {leadTime} day(s)");
            _inner.SendDelayNotification(leadTime, productName);
        }

        public void SendOutOfStockNotification(string productName)
        {
            Capture($"OUT_OF_STOCK| {productName}");
            _inner.SendOutOfStockNotification(productName);
        }

        public void SendExpirationNotification(string productName, DateTime expiryDate)
        {
            Capture($"EXPIRED     | {productName} | expired on {expiryDate:yyyy-MM-dd}");
            _inner.SendExpirationNotification(productName, expiryDate);
        }

        private void Capture(string entry)
        {
            _recorder.Record(entry);
            _logger.LogInformation("Notification sent: {Entry}", entry);
        }
    }
}
