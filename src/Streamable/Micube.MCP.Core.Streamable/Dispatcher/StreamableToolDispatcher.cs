using Micube.MCP.Core.Loader;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Models;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Invokes a tool with streaming support
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> InvokeStreamAsync(
        string fullToolName,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Parse the full tool name (format: "GroupName.ToolName")
        var parts = fullToolName.Split('_',2);
        if (parts.Length != 2)
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Invalid tool name format: '{fullToolName}'. Expected 'GroupName_ToolName'",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow
            };
            yield break;
        }

        var groupName = parts[0];
        var toolName = parts[1];

        // Find the tool group
        if (!_groupMap.TryGetValue(groupName, out var loadedGroup))
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Tool group '{groupName}' not found",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow
            };
            yield break;
        }

        // Log the invocation
        _logger.LogDebug($"Dispatching streamable tool: {fullToolName}");

        // Invoke the tool with streaming
        await foreach (var chunk in loadedGroup.GroupInstance.InvokeStreamAsync(toolName, parameters, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Gets the list of available tool groups
    /// </summary>
    public List<string> GetAvailableGroups()
    {
        return _groupMap.Keys.ToList();
    }

    /// <summary>
    /// Gets metadata for a specific tool group
    /// </summary>
    public ToolGroupMetadata? GetGroupMetadata(string groupName)
    {
        if (_groupMap.TryGetValue(groupName, out var loadedGroup))
        {
            return loadedGroup.Metadata;
        }
        return null;
    }
}