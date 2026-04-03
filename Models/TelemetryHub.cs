using Microsoft.AspNetCore.SignalR;

namespace SmartPotsWeb.Models
{
    public class TelemetryHub : Hub
    {
        private static int _activeViewers = 0;
        public static bool HasViewers => _activeViewers > 0;

        private readonly TelemetryBuffer _buffer;
        private readonly IServiceProvider _serviceProvider;

        public TelemetryHub(TelemetryBuffer buffer, IServiceProvider serviceProvider)
        {
            _buffer = buffer;
            _serviceProvider = serviceProvider;
        }

        public override Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _activeViewers);
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var viewersLeft = Interlocked.Decrement(ref _activeViewers);

            if (viewersLeft == 0)
            {
                await SaveBufferToDatabaseAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task SaveBufferToDatabaseAsync()
        {
            var avgTelemetry = _buffer.CalculateAverageAndClear();
            if (avgTelemetry != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.HubTelemetries.Add(avgTelemetry);
                await db.SaveChangesAsync();
            }
        }
    }
}
