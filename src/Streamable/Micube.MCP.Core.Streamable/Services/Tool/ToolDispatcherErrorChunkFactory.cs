using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public class ToolDispatcherErrorChunkFactory : IToolDispatcherErrorChunkFactory
{
    public StreamChunk CreateToolNameParseErrorChunk(string errorMessage)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = errorMessage,
            IsFinal = true,
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow
        };
    }

    public StreamChunk CreateToolGroupNotFoundChunk(string groupName)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = $"Tool group '{groupName}' not found",
            IsFinal = true,
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow
        };
    }
}