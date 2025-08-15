using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public interface IStreamChunkFactory
{
    StreamChunk CreateMetadataChunk(string toolName, string groupName, Dictionary<string, object> parameters, int sequenceNumber);
    StreamChunk CreateProgressChunk(string content, double? progress, int sequenceNumber);
    StreamChunk CreateCompleteChunk(ToolCallResult result, int sequenceNumber);
    StreamChunk CreateErrorChunk(string errorMessage, int sequenceNumber);
}