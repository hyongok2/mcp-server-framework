using System;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Dispatcher;

public interface IStreamingMessageDispatcher
{
    bool SupportsStreaming(string methodName);
    IAsyncEnumerable<StreamChunk> HandleStreamingAsync(McpMessage message, CancellationToken cancellationToken = default);
    Task<object?> HandleAsync(McpMessage message);
}
