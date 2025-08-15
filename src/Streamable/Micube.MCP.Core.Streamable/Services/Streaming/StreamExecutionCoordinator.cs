using System.Runtime.CompilerServices;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Handlers;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Streaming;

public class StreamExecutionCoordinator : IStreamExecutionCoordinator
{
    private readonly IMcpLogger _logger;

    public StreamExecutionCoordinator(IMcpLogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<StreamChunk> ExecuteStreamingHandlerAsync(
        IStreamingHandler handler,
        McpMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = message.Id?.ToString() ?? "unknown";
        _logger.LogInfo($"Executing streaming method: {message.Method}", requestId);

        var sawFinal = false;
        var lastSequence = 0;

        StreamChunk? terminalChunk = null;
        Exception? storedException = null;
        bool canceled = false;

        var enumerator = handler.HandleStreamingAsync(message, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                StreamChunk currentChunk;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    currentChunk = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                    break;
                }
                catch (Exception ex)
                {
                    storedException = ex;
                    break;
                }

                lastSequence = currentChunk.SequenceNumber;
                yield return currentChunk;

                if (currentChunk.IsFinal || currentChunk.Type == StreamChunkType.Complete || currentChunk.Type == StreamChunkType.Error)
                {
                    sawFinal = true;
                    _logger.LogInfo($"Streaming method completed: {message.Method}", requestId);
                    break;
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (canceled)
        {
            terminalChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Request canceled",
                IsFinal = true,
                SequenceNumber = lastSequence + 1,
                Timestamp = DateTime.UtcNow
            };
        }
        else if (storedException != null)
        {
            _logger.LogError($"Unhandled exception in streaming method '{message.Method}': {storedException.Message}", requestId, storedException);
            terminalChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = storedException.Message,
                IsFinal = true,
                SequenceNumber = lastSequence + 1,
                Timestamp = DateTime.UtcNow
            };
        }

        if (terminalChunk != null)
        {
            yield return terminalChunk;
            yield break;
        }

        // If handler ended without emitting a terminal chunk, emit a default completion
        if (!sawFinal)
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Complete,
                Content = "Operation completed",
                IsFinal = true,
                SequenceNumber = lastSequence + 1,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}