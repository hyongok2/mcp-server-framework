using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Dispatcher;

public interface IStreamableToolDispatcher
{
    /// <summary>
    /// Invokes a tool with streaming support
    /// </summary>
    /// <param name="fullToolName">Full tool name (e.g., "GroupName.ToolName")</param>
    /// <param name="parameters">Tool parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of stream chunks</returns>
    IAsyncEnumerable<StreamChunk> InvokeStreamAsync(string fullToolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available tool groups
    /// </summary>
    List<string> GetAvailableGroups();
    
    /// <summary>
    /// Gets metadata for a specific group
    /// </summary>
    ToolGroupMetadata? GetGroupMetadata(string groupName);
}
