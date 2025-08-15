using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public interface IDispatcherMessageValidator
{
    MessageValidationResult ValidateMessage(McpMessage? message);
    StreamChunk? CreateValidationErrorChunk(MessageValidationResult result);
    object? CreateValidationErrorResponse(MessageValidationResult result, object? messageId);
}

public class MessageValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public object? ErrorResponse { get; init; }

    public static MessageValidationResult Success() => new() { IsValid = true };
    
    public static MessageValidationResult Error(string message, object? response = null) => 
        new() { IsValid = false, ErrorMessage = message, ErrorResponse = response };
}