using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public class DispatcherErrorHandler : IDispatcherErrorHandler
{
    public StreamChunk CreateHandlerNotFoundChunk(string methodName)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = $"Streaming method not found: {methodName}",
            IsFinal = true,
            Timestamp = DateTime.UtcNow
        };
    }

    public StreamChunk CreateStreamingNotSupportedChunk(string methodName)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = $"Method '{methodName}' does not support streaming",
            IsFinal = true,
            Timestamp = DateTime.UtcNow
        };
    }

    public object CreateRegularErrorResponse(object? messageId, string methodName, string errorType, string message)
    {
        return errorType switch
        {
            "MethodNotFound" => ErrorResponseFactory.Create(messageId, McpErrorCodes.METHOD_NOT_FOUND,
                "Method not found", methodName),
            "NotInitialized" => ErrorResponseFactory.Create(messageId, McpErrorCodes.INVALID_REQUEST,
                "Server not initialized", "Call initialize first"),
            "InternalError" => ErrorResponseFactory.Create(messageId, McpErrorCodes.INTERNAL_ERROR,
                "Internal error", message),
            _ => ErrorResponseFactory.Create(messageId, McpErrorCodes.INVALID_REQUEST,
                "Invalid request", message)
        };
    }
}