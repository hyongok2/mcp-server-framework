using System.Runtime.CompilerServices;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services;

public class ToolCallStreamProcessor : IToolCallStreamProcessor
{
    private readonly IStreamableToolDispatcher _streamableToolDispatcher;
    private readonly IMcpResponseWrapper _responseWrapper;

    public ToolCallStreamProcessor(
        IStreamableToolDispatcher streamableToolDispatcher,
        IMcpResponseWrapper responseWrapper)
    {
        _streamableToolDispatcher = streamableToolDispatcher;
        _responseWrapper = responseWrapper;
    }

    public async IAsyncEnumerable<StreamChunk> ProcessToolCallStreamAsync(
        McpToolCallRequest request,
        object messageId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Initial metadata chunk
        yield return _responseWrapper.CreateMetadataChunk(request.Name, request.Arguments);

        var sequenceNumber = 2;
        ToolCallResult? finalResult = null;
        var hasError = false;

        // Execute the tool with streaming support
        await foreach (var chunk in _streamableToolDispatcher.InvokeStreamAsync(
            request.Name,
            request.Arguments ?? new Dictionary<string, object>(),
            cancellationToken))
        {
            // Update sequence number
            chunk.SequenceNumber = sequenceNumber++;

            // Check if this is a completion chunk with result
            if (chunk.Type == StreamChunkType.Complete && chunk.Metadata?.ContainsKey("result") == true)
            {
                // Extract the result from the complete chunk
                finalResult = chunk.Metadata["result"] as ToolCallResult;

                // Wrap the result in MCP 0618-compliant response format
                yield return _responseWrapper.WrapCompleteChunk(chunk, messageId, finalResult);
            }
            else if (chunk.Type == StreamChunkType.Error)
            {
                hasError = true;
                // Pass through error chunks
                yield return chunk;
            }
            else
            {
                // Pass through other chunks (metadata, progress, content)
                yield return chunk;
            }
        }

        // If no final result was received and no error, create a default completion chunk
        if (!hasError && finalResult == null)
        {
            var defaultChunk = _responseWrapper.CreateDefaultCompleteChunk(messageId);
            defaultChunk.SequenceNumber = sequenceNumber;
            yield return defaultChunk;
        }
    }
}