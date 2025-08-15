using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public interface IComponentHealthChecker
{
    string ComponentName { get; }
    Task<HealthStatus> CheckHealthAsync();
}