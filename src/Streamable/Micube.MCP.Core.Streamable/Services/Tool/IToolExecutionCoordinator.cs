using Micube.MCP.Core.Streamable.Models;
using Micube.MCP.SDK.Streamable.Models;
using System.Runtime.CompilerServices;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public interface IToolExecutionCoordinator
{
    IAsyncEnumerable<StreamChunk> ExecuteToolStreamAsync(
        LoadedStreamableToolGroup toolGroup,
        string toolName,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
}