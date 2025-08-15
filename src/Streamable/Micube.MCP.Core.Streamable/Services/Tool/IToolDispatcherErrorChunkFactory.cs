using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public interface IToolDispatcherErrorChunkFactory
{
    StreamChunk CreateToolNameParseErrorChunk(string errorMessage);
    StreamChunk CreateToolGroupNotFoundChunk(string groupName);
}