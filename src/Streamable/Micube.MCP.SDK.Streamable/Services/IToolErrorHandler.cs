using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public interface IToolErrorHandler
{
    StreamChunk HandleToolNotFound(string toolName, string groupName);
    StreamChunk HandleUnsupportedMethod(string toolName, Type returnType);
    StreamChunk HandleInvocationError(string toolName, Exception exception);
}