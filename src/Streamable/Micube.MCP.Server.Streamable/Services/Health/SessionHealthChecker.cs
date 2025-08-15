using Micube.MCP.Core.Session;
using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public class SessionHealthChecker : IComponentHealthChecker
{
    private readonly ISessionState _sessionState;

    public string ComponentName => "session";

    public SessionHealthChecker(ISessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public Task<HealthStatus> CheckHealthAsync()
    {
        var status = new HealthStatus
        {
            Status = "healthy",
            Metadata = new Dictionary<string, object>
            {
                ["initialized"] = _sessionState.IsInitialized,
                ["status"] = _sessionState.IsInitialized ? "initialized" : "not-initialized"
            }
        };

        return Task.FromResult(status);
    }
}