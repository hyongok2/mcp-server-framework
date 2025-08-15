namespace Micube.MCP.SDK.Streamable.Models;

/// <summary>
/// Represents a chunk of streaming data
/// </summary>
public class StreamChunk
{
    /// <summary>
    /// Type of the stream chunk
    /// </summary>
    public StreamChunkType Type { get; set; } = StreamChunkType.Content;

    /// <summary>
    /// Content of the chunk
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the final chunk
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Sequence number for ordering
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Timestamp of the chunk
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Progress information in 0..1 scale (MCP 0618)
    /// </summary>
    public double? Progress { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Types of stream chunks
/// </summary>
public enum StreamChunkType
{
    Content,
    Progress,
    Metadata,
    Error,
    Complete
}