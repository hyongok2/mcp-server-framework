using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public interface IToolGroupRegistry
{
    ToolGroupLookupResult FindGroup(string groupName);
    List<string> GetAvailableGroups();
    ToolGroupMetadata? GetGroupMetadata(string groupName);
}

public class ToolGroupLookupResult
{
    public bool Found { get; init; }
    public LoadedStreamableToolGroup? Group { get; init; }
    public string? ErrorMessage { get; init; }

    public static ToolGroupLookupResult Success(LoadedStreamableToolGroup group) =>
        new() { Found = true, Group = group };

    public static ToolGroupLookupResult NotFound(string groupName) =>
        new() { Found = false, ErrorMessage = $"Tool group '{groupName}' not found" };
}