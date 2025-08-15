using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services;

public interface IMcpResponseWrapper
{
    StreamChunk CreateMetadataChunk(string toolName, Dictionary<string, object>? arguments);
    StreamChunk WrapCompleteChunk(StreamChunk originalChunk, object messageId, ToolCallResult? result);
    StreamChunk CreateDefaultCompleteChunk(object messageId);
}