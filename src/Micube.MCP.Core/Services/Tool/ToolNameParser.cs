using Micube.MCP.Core.Utils;

namespace Micube.MCP.Core.Services.Tool;

public class ToolNameParser : IToolNameParser
{
    public ToolNameParseResult ParseToolName(string fullToolName)
    {
        if (string.IsNullOrWhiteSpace(fullToolName))
        {
            return ToolNameParseResult.Error("Tool name cannot be null or empty");
        }

        // Parse the full tool name (format: "GroupName_ToolName")
        var parts = fullToolName.Split(ToolConstants.NameSeparator, 2);
        if (parts.Length != 2)
        {
            return ToolNameParseResult.Error($"Invalid tool name format: '{fullToolName}'. Expected 'GroupName{ToolConstants.NameSeparator}ToolName'");
        }

        var groupName = parts[0];
        var toolName = parts[1];

        if (string.IsNullOrWhiteSpace(groupName))
        {
            return ToolNameParseResult.Error("Group name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return ToolNameParseResult.Error("Tool name cannot be empty");
        }

        return ToolNameParseResult.Success(groupName, toolName);
    }
}