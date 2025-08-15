using System.Reflection;
using System.Runtime.CompilerServices;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public interface IMethodInvocationStrategy
{
    bool CanHandle(MethodInfo method);
    IAsyncEnumerable<StreamChunk> InvokeAsync(
        MethodInfo method, 
        object instance, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken,
        IStreamChunkFactory chunkFactory);
}