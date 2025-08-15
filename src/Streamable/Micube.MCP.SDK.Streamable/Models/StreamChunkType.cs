namespace Micube.MCP.SDK.Streamable.Models;

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