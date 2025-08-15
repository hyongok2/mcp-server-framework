using System.Runtime.CompilerServices;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Handlers;

/// <summary>
/// Handler interface that extends existing IMethodHandler for streaming support
/// </summary>
public interface IStreamingHandler : IMethodHandler
{
    /// <summary>
    /// Indicates if this handler supports streaming responses
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Handle streaming requests with real-time response capability
    /// </summary>
    IAsyncEnumerable<StreamChunk> HandleStreamingAsync(
        McpMessage message, 
        CancellationToken cancellationToken = default);
}