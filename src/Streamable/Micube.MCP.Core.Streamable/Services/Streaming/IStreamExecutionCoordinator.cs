using System.Runtime.CompilerServices;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Handlers;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Streaming;

public interface IStreamExecutionCoordinator
{
    IAsyncEnumerable<StreamChunk> ExecuteStreamingHandlerAsync(
        IStreamingHandler handler,
        McpMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
}