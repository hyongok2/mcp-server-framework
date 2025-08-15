using Micube.MCP.Core.Loader;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Models;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using System.Reflection;

namespace Micube.MCP.Core.Streamable.Dispatcher;

/// <summary>
/// Service to load and dispatch streamable tools
/// </summary>
public class StreamableToolDispatcher : IStreamableToolDispatcher
{
    private readonly IMcpLogger _logger;
    private readonly Dictionary<string, LoadedStreamableToolGroup> _groupMap;

    public StreamableToolDispatcher(
        IEnumerable<LoadedStreamableToolGroup> loadedGroups,
        IMcpLogger logger)
    {
        _logger = logger;
        _groupMap = loadedGroups.ToDictionary(
            g => g.GroupName,
            g => g,
            StringComparer.OrdinalIgnoreCase);
    }

    // TODO: 아래 함수를 변경해야 함. 스트리밍이 가능한 방식으로. 
    public Task<ToolCallResult> InvokeAsync(string fullToolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public List<string> GetAvailableGroups()
    {
        throw new NotImplementedException();
    }

    public ToolGroupMetadata? GetGroupMetadata(string groupName)
    {
        throw new NotImplementedException();
    }
}