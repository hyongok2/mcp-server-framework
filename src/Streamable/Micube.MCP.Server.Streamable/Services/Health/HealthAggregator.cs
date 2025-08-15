using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public class HealthAggregator : IHealthAggregator
{
    private readonly IEnumerable<IComponentHealthChecker> _healthCheckers;

    public HealthAggregator(IEnumerable<IComponentHealthChecker> healthCheckers)
    {
        _healthCheckers = healthCheckers;
    }

    public Task<OverallHealth> GetBasicHealthAsync()
    {
        var health = new OverallHealth
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = "0.1.0",
            Components = new List<ComponentHealth>()
        };

        return Task.FromResult(health);
    }

    public async Task<OverallHealth> GetDetailedHealthAsync()
    {
        var components = new List<ComponentHealth>();

        foreach (var checker in _healthCheckers)
        {
            var componentStatus = await checker.CheckHealthAsync();
            components.Add(new ComponentHealth
            {
                Name = checker.ComponentName,
                Status = componentStatus
            });
        }

        var overallStatus = components.All(c => c.Status.IsHealthy) ? "healthy" : "unhealthy";

        return new OverallHealth
        {
            Status = overallStatus,
            Timestamp = DateTime.UtcNow,
            Version = "0.1.0",
            Components = components
        };
    }
}