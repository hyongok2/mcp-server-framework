using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public class HealthResponseFormatter : IHealthResponseFormatter
{
    public object FormatBasicResponse(OverallHealth health)
    {
        return new
        {
            status = health.Status,
            timestamp = health.Timestamp,
            version = health.Version
        };
    }

    public object FormatDetailedResponse(OverallHealth health)
    {
        return new
        {
            status = health.Status,
            timestamp = health.Timestamp,
            version = health.Version,
            components = health.Components.ToDictionary(
                c => c.Name,
                c => FormatComponentHealth(c.Status)
            )
        };
    }

    private object FormatComponentHealth(HealthStatus status)
    {
        var result = new Dictionary<string, object>
        {
            ["status"] = status.Status,
            ["healthy"] = status.IsHealthy
        };

        if (!string.IsNullOrEmpty(status.Error))
        {
            result["error"] = status.Error;
        }

        if (status.Metadata != null)
        {
            foreach (var metadata in status.Metadata)
            {
                result[metadata.Key] = metadata.Value;
            }
        }

        return result;
    }
}