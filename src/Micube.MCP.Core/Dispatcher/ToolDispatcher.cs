using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services.Tool;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Dispatcher;

public class ToolDispatcher : IToolDispatcher
{
    private readonly IToolNameParser _toolNameParser;
    private readonly Dictionary<string, LoadedToolGroup> _groupMap;
    private readonly IMcpLogger _logger;

    public ToolDispatcher(IEnumerable<LoadedToolGroup> loadedGroups, IMcpLogger logger, IToolNameParser toolNameParser)
    {
        _logger = logger;
        _groupMap = loadedGroups.ToDictionary(
            g => g.GroupName,
            g => g,
            StringComparer.OrdinalIgnoreCase);
        _toolNameParser = toolNameParser;
    }

    public async Task<ToolCallResult> InvokeAsync(string fullToolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        // Parse tool name
        var parseResult = _toolNameParser.ParseToolName(fullToolName);
        if (!parseResult.IsValid)
        {
            _logger.LogError($"Invalid tool name format: {fullToolName}");
            return ToolCallResult.Fail("Invalid tool name format: {fullToolName}");
        }
        
        var groupName = parseResult.GroupName;
        var toolName = parseResult.ToolName;

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