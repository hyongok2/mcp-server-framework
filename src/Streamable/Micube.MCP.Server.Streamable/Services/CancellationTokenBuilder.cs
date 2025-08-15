using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public class CancellationTokenBuilder : ICancellationTokenBuilder
{
    public CancellationTokenContext CreateStreamingCancellationTokens(HttpContext httpContext, TimeSpan streamTimeout)
    {
        var timeoutCts = new CancellationTokenSource(streamTimeout);
        var requestLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, timeoutCts.Token);
        var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(requestLinkedCts.Token);

        return new CancellationTokenContext(timeoutCts, requestLinkedCts, heartbeatCts);
    }
}