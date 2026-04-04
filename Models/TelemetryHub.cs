using Microsoft.AspNetCore.SignalR;

namespace SmartPotsWeb.Models
{
    public class TelemetryHub : Hub
    {
        private static int _activeViewers = 0;
        public static bool HasViewers => Interlocked.CompareExchange(ref _activeViewers, 0, 0) > 0;
        public TelemetryHub()
        {
        }

        public override Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _activeViewers);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Interlocked.Decrement(ref _activeViewers);
            return base.OnDisconnectedAsync(exception);
        }
    }
}