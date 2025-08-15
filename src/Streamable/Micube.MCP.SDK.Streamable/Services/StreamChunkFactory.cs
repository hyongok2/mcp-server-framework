using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public class StreamChunkFactory : IStreamChunkFactory
{
    public StreamChunk CreateMetadataChunk(string toolName, string groupName, Dictionary<string, object> parameters, int sequenceNumber)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Metadata,
            Content = $"Starting tool: {toolName}",
            SequenceNumber = sequenceNumber,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["tool"] = toolName,
                ["group"] = groupName,
                ["parameters"] = parameters
            }
        };
    }

    public StreamChunk CreateProgressChunk(string content, double? progress, int sequenceNumber)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Progress,
            Content = content,
            SequenceNumber = sequenceNumber,
            Progress = progress,
            Timestamp = DateTime.UtcNow
        };
    }

    public StreamChunk CreateCompleteChunk(ToolCallResult result, int sequenceNumber)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = "Tool execution completed",
            IsFinal = true,
            SequenceNumber = sequenceNumber,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["result"] = result
            }
        };
    }

    public StreamChunk CreateErrorChunk(string errorMessage, int sequenceNumber)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = errorMessage,
            IsFinal = true,
            SequenceNumber = sequenceNumber,
            Timestamp = DateTime.UtcNow
        };
    }
}