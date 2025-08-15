using System.Runtime.CompilerServices;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services;

public interface IToolCallStreamProcessor
{
    IAsyncEnumerable<StreamChunk> ProcessToolCallStreamAsync(
        McpToolCallRequest request,
        object messageId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
}