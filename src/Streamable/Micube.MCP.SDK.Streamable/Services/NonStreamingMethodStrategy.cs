using System.Reflection;
using System.Runtime.CompilerServices;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public class NonStreamingMethodStrategy : IMethodInvocationStrategy
{
    public bool CanHandle(MethodInfo method)
    {
        return method.ReturnType == typeof(Task<ToolCallResult>);
    }

    public async IAsyncEnumerable<StreamChunk> InvokeAsync(
        MethodInfo method, 
        object instance, 
        Dictionary<string, object> parameters, 
        [EnumeratorCancellation] CancellationToken cancellationToken,
        IStreamChunkFactory chunkFactory)
    {
        var task = (Task<ToolCallResult>)method.Invoke(instance, new object[] { parameters })!;
        var result = await task;

        // Progress chunk
        yield return chunkFactory.CreateProgressChunk("Processing...", 0.5, 2);

        // Complete chunk with result
        yield return chunkFactory.CreateCompleteChunk(result, 3);
    }
}