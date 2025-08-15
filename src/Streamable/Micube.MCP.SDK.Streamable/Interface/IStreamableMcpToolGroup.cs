using System;
using System.Text.Json;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Interface;

public interface IStreamableMcpToolGroup
{
    /// <summary>
    /// 이 Tool 그룹의 이름
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// Invokes a tool with streaming support
    /// </summary>
    /// <param name="toolName">Tool name within this group</param>
    /// <param name="parameters">Tool parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of stream chunks</returns>
    IAsyncEnumerable<StreamChunk> InvokeStreamAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary> ToolGroup 자체 Config 주입 </summary>
    void Configure(JsonElement? config);
}
