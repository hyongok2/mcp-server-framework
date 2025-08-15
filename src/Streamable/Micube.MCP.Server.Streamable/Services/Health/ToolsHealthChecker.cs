using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public class ToolsHealthChecker : IComponentHealthChecker
{
    private readonly IStreamableToolDispatcher _toolDispatcher;
    private readonly IMcpLogger _logger;

    public string ComponentName => "tools";

    public ToolsHealthChecker(IStreamableToolDispatcher toolDispatcher, IMcpLogger logger)
    {
        _toolDispatcher = toolDispatcher;
        _logger = logger;
    }

    public async Task<HealthStatus> CheckHealthAsync()
    {
        try
        {
            var groups = _toolDispatcher.GetAvailableGroups();
            
            return new HealthStatus
            {
                Status = "healthy",
                Metadata = new Dictionary<string, object>
                {
                    ["toolGroupsCount"] = groups.Count,
                    ["groups"] = groups
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Tools health check failed: {ex.Message}", ex);
            
            return new HealthStatus
            {
                Status = "unhealthy",
                Error = ex.Message
            };
        }
    }
}