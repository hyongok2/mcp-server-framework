using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Dispatcher;

public class ToolDispatcher : IToolDispatcher
{
    private readonly Dictionary<string, LoadedToolGroup> _groupMap;
    private readonly IMcpLogger _logger;

    public ToolDispatcher(IEnumerable<LoadedToolGroup> loadedGroups, IMcpLogger logger)
    {
        _logger = logger;
        _groupMap = loadedGroups.ToDictionary(
            g => g.GroupName,
            g => g,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ToolCallResult> InvokeAsync(string fullToolName, Dictionary<string, object> parameters)
    {
        var parts = fullToolName.Split('.', 2);
        if (parts.Length != 2)
            return ToolCallResult.Fail("Invalid tool name format. Expected 'GroupName.ToolName'.");

        var groupName = parts[0];
        var toolName = parts[1];

        if (!_groupMap.TryGetValue(groupName, out var group))
        {
            _logger.LogError($"ToolGroup not found: {groupName}");
            return ToolCallResult.Fail($"ToolGroup '{groupName}' not found.");
        }

        try
        {
            return await group.GroupInstance.InvokeAsync(toolName, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error invoking tool '{toolName}' in group '{groupName}': {ex.Message}", ex);
            return ToolCallResult.Fail($"Unhandled exception: {ex.Message}");
        }
    }

    public List<string> GetAvailableGroups() => _groupMap.Keys.ToList();

    public ToolGroupMetadata? GetGroupMetadata(string groupName)
    {
        return _groupMap.TryGetValue(groupName, out var group)
            ? group.Metadata
            : null;
    }
}