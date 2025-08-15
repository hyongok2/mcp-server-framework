namespace Micube.MCP.Server.Streamable.Services.Health.Models;

public class HealthStatus
{
    public string Status { get; set; } = "healthy";
    public bool IsHealthy => Status == "healthy";
    public string? Error { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = new();
}

public class OverallHealth
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "0.1.0";
    public List<ComponentHealth> Components { get; set; } = new();
    public bool IsHealthy => Status == "healthy" && Components.All(c => c.Status.IsHealthy);
}