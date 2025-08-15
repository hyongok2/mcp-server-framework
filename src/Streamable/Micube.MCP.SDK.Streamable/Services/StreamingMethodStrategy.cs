using System.Reflection;
using System.Runtime.CompilerServices;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public class StreamingMethodStrategy : IMethodInvocationStrategy
{
    public bool CanHandle(MethodInfo method)
    {
        var returnType = method.ReturnType;
        return returnType.IsGenericType &&
               returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
               returnType.GetGenericArguments()[0] == typeof(StreamChunk);
    }

    public async IAsyncEnumerable<StreamChunk> InvokeAsync(
        MethodInfo method, 
        object instance, 
        Dictionary<string, object> parameters, 
        [EnumeratorCancellation] CancellationToken cancellationToken,
        IStreamChunkFactory chunkFactory)
    {
        var result = method.Invoke(instance, new object[] { parameters, cancellationToken });
        if (result is IAsyncEnumerable<StreamChunk> stream)
        {
            var sequenceNumber = 2;
            await foreach (var chunk in stream.WithCancellation(cancellationToken))
            {
                chunk.SequenceNumber = sequenceNumber++;
                yield return chunk;
            }
        }
    }
}