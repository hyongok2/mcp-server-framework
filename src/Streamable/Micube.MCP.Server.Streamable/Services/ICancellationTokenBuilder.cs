using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public interface ICancellationTokenBuilder
{
    CancellationTokenContext CreateStreamingCancellationTokens(HttpContext httpContext, TimeSpan streamTimeout);
}

public class CancellationTokenContext : IDisposable
{
    public CancellationToken ServerToken { get; init; }
    public CancellationToken HeartbeatToken { get; init; }
    
    private readonly CancellationTokenSource _timeoutCts;
    private readonly CancellationTokenSource _requestLinkedCts;
    private readonly CancellationTokenSource _heartbeatCts;

    public CancellationTokenContext(
        CancellationTokenSource timeoutCts, 
        CancellationTokenSource requestLinkedCts, 
        CancellationTokenSource heartbeatCts)
    {
        _timeoutCts = timeoutCts;
        _requestLinkedCts = requestLinkedCts;
        _heartbeatCts = heartbeatCts;
        
        ServerToken = _requestLinkedCts.Token;
        HeartbeatToken = _heartbeatCts.Token;
    }

    public void CancelHeartbeat() => _heartbeatCts.Cancel();

    public void Dispose()
    {
        _timeoutCts.Dispose();
        _requestLinkedCts.Dispose();
        _heartbeatCts.Dispose();
    }
}