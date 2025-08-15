using System.Runtime.CompilerServices;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Services.Dispatcher;
using Micube.MCP.Core.Streamable.Services.Streaming;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Dispatcher;

/// <summary>
/// Message dispatcher with streaming capabilities
/// </summary>
public class StreamingMessageDispatcher : IStreamingMessageDispatcher
{
    private readonly IDispatcherMessageValidator _messageValidator;
    private readonly IHandlerRegistry _handlerRegistry;
    private readonly ISessionValidator _sessionValidator;
    private readonly IStreamExecutionCoordinator _streamCoordinator;
    private readonly IDispatcherErrorHandler _errorHandler;
    private readonly IMcpLogger _logger;

    public StreamingMessageDispatcher(
        IDispatcherMessageValidator messageValidator,
        IHandlerRegistry handlerRegistry,
        ISessionValidator sessionValidator,
        IStreamExecutionCoordinator streamCoordinator,
        IDispatcherErrorHandler errorHandler,
        IMcpLogger logger)
    {
        _messageValidator = messageValidator;
        _handlerRegistry = handlerRegistry;
        _sessionValidator = sessionValidator;
        _streamCoordinator = streamCoordinator;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    /// <summary>
    /// Check if a method supports streaming
    /// </summary>
    public bool SupportsStreaming(string methodName)
    {
        return _handlerRegistry.SupportsStreaming(methodName);
    }

    /// <summary>
    /// Handle streaming requests
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> HandleStreamingAsync(
        McpMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = message?.Id?.ToString() ?? "unknown";
        _logger.LogDebug($"Received streaming message: {message?.Method}", requestId);

        // Message validation
        var validation = _messageValidator.ValidateMessage(message);
        if (!validation.IsValid)
        {
            _logger.LogError($"Streaming message validation failed", requestId);
            var errorChunk = _messageValidator.CreateValidationErrorChunk(validation);
            if (errorChunk != null) yield return errorChunk;
            yield break;
        }

        // Find streaming handler
        var handlerResult = _handlerRegistry.FindHandler(message!.Method!, requiresStreaming: true);
        if (!handlerResult.Found)
        {
            _logger.LogError($"Streaming method not found: {message.Method}", requestId);
            yield return _errorHandler.CreateHandlerNotFoundChunk(message.Method!);
            yield break;
        }

        if (!handlerResult.SupportsStreaming)
        {
            _logger.LogError($"Method '{message.Method}' does not support streaming", requestId);
            yield return _errorHandler.CreateStreamingNotSupportedChunk(message.Method!);
            yield break;
        }

        // Session state check
        var sessionResult = _sessionValidator.ValidateSession(handlerResult.Handler!);
        if (!sessionResult.IsValid)
        {
            _logger.LogError($"Streaming method '{message.Method}' called before initialization", requestId);
            var errorChunk = _sessionValidator.CreateSessionErrorChunk(sessionResult, message.Method!);
            if (errorChunk != null) yield return errorChunk;
            yield break;
        }

        // Execute streaming handler
        await foreach (var chunk in _streamCoordinator.ExecuteStreamingHandlerAsync(
            handlerResult.StreamingHandler!, message, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Handle regular (non-streaming) requests - delegates to standard handler logic
    /// </summary>
    public async Task<object?> HandleAsync(McpMessage message)
    {
        var requestId = message?.Id?.ToString() ?? "unknown";
        _logger.LogDebug($"Received message: {message?.Method}", requestId);

        // Message validation
        var validation = _messageValidator.ValidateMessage(message);
        if (!validation.IsValid)
        {
            _logger.LogError($"Message validation failed", requestId);
            return _messageValidator.CreateValidationErrorResponse(validation, message?.Id);
        }

        // Find handler
        var handlerResult = _handlerRegistry.FindHandler(message!.Method!, requiresStreaming: false);
        if (!handlerResult.Found)
        {
            _logger.LogError($"Method not found: {message.Method}", requestId);
            return _errorHandler.CreateRegularErrorResponse(message.Id, message.Method!, "MethodNotFound", message.Method!);
        }

        // Session state check
        var sessionResult = _sessionValidator.ValidateSession(handlerResult.Handler!);
        if (!sessionResult.IsValid)
        {
            _logger.LogError($"Method '{message.Method}' called before initialization", requestId);
            return _errorHandler.CreateRegularErrorResponse(message.Id, message.Method!, "NotInitialized", "Call initialize first");
        }

        try
        {
            // Execute handler
            _logger.LogInfo($"Executing method: {message.Method}", requestId);
            var result = await handlerResult.Handler!.HandleAsync(message);

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
            return _errorHandler.CreateRegularErrorResponse(message.Id, message.Method!, "InternalError", ex.Message);
        }
    }

}
