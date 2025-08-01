using System;

namespace Micube.MCP.Core.Services;

public class CapabilitiesValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static CapabilitiesValidationResult Success() => new() { IsValid = true };
    public static CapabilitiesValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
}