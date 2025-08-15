using System.Runtime.CompilerServices;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Streamable.Handlers;
using Micube.MCP.Core.Utils;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Dispatcher;

/// <summary>
/// Message dispatcher with streaming capabilities
/// </summary>
public class StreamingMessageDispatcher : IStreamingMessageDispatcher
{
    private readonly IMessageValidator _validator;
    private readonly ISessionState _sessionState;
    private readonly Dictionary<string, IMethodHandler> _handlers;
    private readonly Dictionary<string, IStreamingHandler> _streamingHandlers;
    private readonly IMcpLogger _logger;

    public StreamingMessageDispatcher(
        IMessageValidator validator,
        ISessionState sessionState,
        IEnumerable<IMethodHandler> handlers,
        IMcpLogger logger)
    {
        _validator = validator;
        _sessionState = sessionState;
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.MethodName, h => h, StringComparer.OrdinalIgnoreCase);

        // Extract streaming-capable handlers
        _streamingHandlers = handlers
            .OfType<IStreamingHandler>()
            .ToDictionary(h => h.MethodName, h => h, StringComparer.OrdinalIgnoreCase);

        _logger.LogInfo($"StreamingMessageDispatcher initialized with {_handlers.Count} handlers, {_streamingHandlers.Count} support streaming");
        foreach (var handler in _streamingHandlers)
        {
            _logger.LogDebug($"  - {handler.Key} (Streaming: {handler.Value.SupportsStreaming})");
        }
    }

    /// <summary>
    /// Check if a method supports streaming
    /// </summary>
    public bool SupportsStreaming(string methodName)
    {
        return _streamingHandlers.TryGetValue(methodName, out var handler) &&
               handler.SupportsStreaming;
    }

    /// <summary>
    /// Handle streaming requests
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> HandleStreamingAsync(
        McpMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Message cannot be null",
                IsFinal = true
            };
            yield break;
        }

        var requestId = message.Id?.ToString() ?? "unknown";
        _logger.LogDebug($"Received streaming message: {message.Method}", requestId);

        // Message validation
        var validation = _validator.Validate(message);
        if (!validation.IsValid)
        {
            _logger.LogError($"Streaming message validation failed", requestId);
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = validation.ErrorResponse?.Error?.Message ?? "Validation failed",
                IsFinal = true
            };
            yield break;
        }

        // Find streaming handler
        if (!_streamingHandlers.TryGetValue(message.Method!, out var handler))
        {
            _logger.LogError($"Streaming method not found: {message.Method}", requestId);
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Streaming method not found: {message.Method}",
                IsFinal = true
            };
            yield break;
        }

        // Check if handler supports streaming
        if (!handler.SupportsStreaming)
        {
            _logger.LogError($"Method '{message.Method}' does not support streaming", requestId);
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Method '{message.Method}' does not support streaming",
                IsFinal = true
            };
            yield break;
        }

        // Session state check
        if (handler.RequiresInitialization && !_sessionState.IsInitialized)
        {
            _logger.LogError($"Streaming method '{message.Method}' called before initialization", requestId);
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Server not initialized. Call initialize first",
                IsFinal = true
            };
            yield break;
        }

        // Execute streaming handler
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

    /// <summary>
    /// Handle regular (non-streaming) requests - delegates to standard handler logic
    /// </summary>
    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message == null)
        {
            _logger.LogError("Received null message");
            return ErrorResponseFactory.Create(null, McpErrorCodes.INVALID_REQUEST,
                "Invalid request", "Message cannot be null");
        }

        var requestId = message.Id ?? "unknown";
        _logger.LogDebug($"Received message: {message.Method}", requestId);

        // Message validation
        var validation = _validator.Validate(message);
        if (!validation.IsValid)
        {
            if (validation.ErrorResponse != null)
            {
                validation.ErrorResponse.Id = message?.Id;
                _logger.LogError($"Message validation failed: {validation.ErrorResponse.Error?.Message}", requestId);
            }
            return validation.ErrorResponse;
        }

        // Find handler
        if (!_handlers.TryGetValue(message.Method!, out var handler))
        {
            _logger.LogError($"Method not found: {message.Method}", requestId);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.METHOD_NOT_FOUND,
                "Method not found", message.Method);
        }

        // Initialization state check
        if (handler.RequiresInitialization && !_sessionState.IsInitialized)
        {
            _logger.LogError($"Method '{message.Method}' called before initialization", requestId);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_REQUEST,
                "Server not initialized", "Call initialize first");
        }

        try
        {
            // Execute handler
            _logger.LogInfo($"Executing method: {message.Method}", requestId);
            var result = await handler.HandleAsync(message);

            if (result == null)
            {
                _logger.LogDebug($"Notification processed: {message.Method}", requestId);
                return null; // No response for notifications
            }

            _logger.LogInfo($"Method completed: {message.Method}", requestId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception in method '{message.Method}': {ex.Message}", requestId, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Internal error", ex.Message);
        }
    }

}
