using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Server.Streamable.Services;

public interface ISseFormatter
{
    string FormatAsSSE(McpMessage request, StreamChunk chunk);
}