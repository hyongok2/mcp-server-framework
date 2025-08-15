using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public class ToolGroupRegistry : IToolGroupRegistry
{
    private readonly Dictionary<string, LoadedStreamableToolGroup> _groupMap;

    public ToolGroupRegistry(IEnumerable<LoadedStreamableToolGroup> loadedGroups)
    {
        _groupMap = loadedGroups.ToDictionary(
            g => g.GroupName,
            g => g,
            StringComparer.OrdinalIgnoreCase);
    }

    public ToolGroupLookupResult FindGroup(string groupName)
    {
        if (_groupMap.TryGetValue(groupName, out var loadedGroup))
        {
            return ToolGroupLookupResult.Success(loadedGroup);
        }
        
        return ToolGroupLookupResult.NotFound(groupName);
    }

    public List<string> GetAvailableGroups()
    {
        return _groupMap.Keys.ToList();
    }

    public ToolGroupMetadata? GetGroupMetadata(string groupName)
    {
        if (_groupMap.TryGetValue(groupName, out var loadedGroup))
        {
            return loadedGroup.Metadata;
        }
        return null;
    }
}