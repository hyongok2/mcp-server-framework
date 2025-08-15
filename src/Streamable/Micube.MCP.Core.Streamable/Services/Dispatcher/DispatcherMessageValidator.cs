using Micube.MCP.Core.Models;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public class DispatcherMessageValidator : IDispatcherMessageValidator
{
    private readonly IMessageValidator _validator;

    public DispatcherMessageValidator(IMessageValidator validator)
    {
        _validator = validator;
    }

    public MessageValidationResult ValidateMessage(McpMessage? message)
    {
        if (message == null)
        {
            return MessageValidationResult.Error("Message cannot be null");
        }

        var validation = _validator.Validate(message);
        if (!validation.IsValid)
        {
            var errorMessage = validation.ErrorResponse?.Error?.Message ?? "Validation failed";
            return MessageValidationResult.Error(errorMessage, validation.ErrorResponse);
        }

        return MessageValidationResult.Success();
    }

    public StreamChunk? CreateValidationErrorChunk(MessageValidationResult result)
    {
        if (result.IsValid)
            return null;

        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = result.ErrorMessage ?? "Validation failed",
            IsFinal = true,
            Timestamp = DateTime.UtcNow
        };
    }

    public object? CreateValidationErrorResponse(MessageValidationResult result, object? messageId)
    {
        if (result.IsValid)
            return null;

        if (result.ErrorResponse != null)
        {
            // If the validation already has an error response, use it and set the message ID
            if (result.ErrorResponse is McpErrorResponse errorResponse)
            {
                errorResponse.Id = messageId;
            }
            return result.ErrorResponse;
        }

        // Create a generic validation error
        return new McpErrorResponse
        {
            Id = messageId,
            Error = new McpError
            {
                Code = -32600,
                Message = "Invalid Request",
                Data = result.ErrorMessage ?? "Validation failed"
            }
        };
    }
}