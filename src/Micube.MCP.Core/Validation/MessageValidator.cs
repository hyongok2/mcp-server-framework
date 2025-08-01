using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;

namespace Micube.MCP.Core.Validation;

public class MessageValidator : IMessageValidator
{
    public ValidationResult Validate(McpMessage message)
    {
        if (message == null)
            return ValidationResult.Error(McpErrorCodes.PARSE_ERROR, "Invalid JSON", "Failed to parse JSON-RPC message");

        if (message.JsonRpc != JsonRpcConstants.Version)
            return ValidationResult.Error(McpErrorCodes.INVALID_REQUEST, "Invalid JSON-RPC version", "Only version 2.0 is supported");

        if (string.IsNullOrEmpty(message.Method))
            return ValidationResult.Error(McpErrorCodes.INVALID_REQUEST, "Missing method", "Method field is required");

        return ValidationResult.Success();
    }
}
