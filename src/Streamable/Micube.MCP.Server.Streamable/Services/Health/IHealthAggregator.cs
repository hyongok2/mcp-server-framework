using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public interface IHealthAggregator
{
    Task<OverallHealth> GetBasicHealthAsync();
    Task<OverallHealth> GetDetailedHealthAsync();
}