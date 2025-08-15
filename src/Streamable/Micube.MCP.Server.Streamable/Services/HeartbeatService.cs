using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public class HeartbeatService : IHeartbeatService
{
    public async Task StartHeartbeatAsync(HttpResponse response, TimeSpan interval, CancellationToken cancellationToken)
    {
        using var heartbeatTimer = new PeriodicTimer(interval);
        
        try
        {
            while (await heartbeatTimer.WaitForNextTickAsync(cancellationToken))
            {
                await response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
    }
}