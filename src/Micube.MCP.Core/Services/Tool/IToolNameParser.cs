namespace Micube.MCP.Core.Services.Tool;

public interface IToolNameParser
{
    ToolNameParseResult ParseToolName(string fullToolName);
}

public class ToolNameParseResult
{
    public bool IsValid { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static ToolNameParseResult Success(string groupName, string toolName) =>
        new() { IsValid = true, GroupName = groupName, ToolName = toolName };

    public static ToolNameParseResult Error(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}