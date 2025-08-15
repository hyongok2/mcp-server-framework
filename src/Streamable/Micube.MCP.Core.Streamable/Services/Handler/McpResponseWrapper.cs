using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Handler;

public class McpResponseWrapper : IMcpResponseWrapper
{
    public StreamChunk CreateMetadataChunk(string toolName, Dictionary<string, object>? arguments)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Metadata,
            Content = "tools/call started",
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["tool"] = toolName,
                ["argumentsPreview"] = string.Join(",", (arguments?.Keys ?? Enumerable.Empty<string>()))
            }
        };
    }

    public StreamChunk WrapCompleteChunk(StreamChunk originalChunk, object messageId, ToolCallResult? result)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = originalChunk.Content,
            IsFinal = true,
            SequenceNumber = originalChunk.SequenceNumber,
            Timestamp = originalChunk.Timestamp,
            Progress = originalChunk.Progress,
            Metadata = new Dictionary<string, object>
            {
                ["response"] = new
                {
                    jsonrpc = "2.0",
                    id = messageId,
                    result = result
                }
            }
        };
    }

    public StreamChunk CreateDefaultCompleteChunk(object messageId)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = "Tool execution completed",
            IsFinal = true,
            SequenceNumber = 0, // Will be set by caller
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["response"] = new
                {
                    jsonrpc = "2.0",
                    id = messageId,
                    result = new ToolCallResult
                    {
                        Content = new List<ToolContent>
                        {
                            new ToolContent { Type = "text", Text = "Tool completed successfully" }
                        }
                    }
                }
            }
        };
    }
}