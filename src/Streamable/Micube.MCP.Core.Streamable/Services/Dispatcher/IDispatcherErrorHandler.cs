using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public interface IDispatcherErrorHandler
{
    StreamChunk CreateHandlerNotFoundChunk(string methodName);
    StreamChunk CreateStreamingNotSupportedChunk(string methodName);
    object CreateRegularErrorResponse(object? messageId, string methodName, string errorType, string message);
}