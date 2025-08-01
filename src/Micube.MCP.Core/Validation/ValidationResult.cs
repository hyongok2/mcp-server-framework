using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;

namespace Micube.MCP.Core.Validation;

public class ValidationResult
{
    public bool IsValid { get; init; }
    public McpErrorResponse? ErrorResponse { get; init; }

    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Error(int code, string message, object? data = null) => new()
    {
        IsValid = false,
        ErrorResponse = ErrorResponseFactory.Create(null, code, message, data)
    };
}
