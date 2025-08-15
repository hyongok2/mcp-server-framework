namespace Micube.MCP.Server.Streamable.Options;

/// <summary>
/// Configuration options for the streamable MCP server
/// </summary>
public class StreamableServerOptions
{
    public const string SectionName = "StreamableServer";

    /// <summary>
    /// Maximum number of concurrent streaming connections
    /// </summary>
    public int MaxConcurrentStreams { get; set; } = 100;

    /// <summary>
    /// Timeout for streaming operations
    /// </summary>
    public TimeSpan StreamTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Enable CORS for cross-origin requests
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Enable detailed request/response logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Maximum request body size in bytes
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Keep alive timeout for HTTP connections
    /// </summary>
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable Swagger UI in development
    /// </summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Enable periodic SSE heartbeat comments to keep connection alive
    /// </summary>
    public bool EnableHeartbeat { get; set; } = true;

    /// <summary>
    /// Heartbeat interval
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);
}

