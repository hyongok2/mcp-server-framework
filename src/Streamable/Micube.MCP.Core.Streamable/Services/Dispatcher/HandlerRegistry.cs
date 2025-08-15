using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Streamable.Handlers;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public class HandlerRegistry : IHandlerRegistry
{
    private readonly Dictionary<string, IMethodHandler> _handlers;
    private readonly Dictionary<string, IStreamingHandler> _streamingHandlers;
    private readonly IMcpLogger _logger;

    public HandlerRegistry(IEnumerable<IMethodHandler> handlers, IMcpLogger logger)
    {
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.MethodName, h => h, StringComparer.OrdinalIgnoreCase);

        // Extract streaming-capable handlers
        _streamingHandlers = handlers
            .OfType<IStreamingHandler>()
            .ToDictionary(h => h.MethodName, h => h, StringComparer.OrdinalIgnoreCase);

        LogHandlerRegistration();
    }

    public bool SupportsStreaming(string methodName)
    {
        return _streamingHandlers.TryGetValue(methodName, out var handler) &&
               handler.SupportsStreaming;
    }

    public IMethodHandler? GetHandler(string methodName)
    {
        _handlers.TryGetValue(methodName, out var handler);
        return handler;
    }

    public IStreamingHandler? GetStreamingHandler(string methodName)
    {
        _streamingHandlers.TryGetValue(methodName, out var handler);
        return handler;
    }

    public HandlerLookupResult FindHandler(string methodName, bool requiresStreaming = false)
    {
        if (requiresStreaming)
        {
            if (!_streamingHandlers.TryGetValue(methodName, out var streamingHandler))
            {
                return HandlerLookupResult.NotFound(methodName);
            }

            if (!streamingHandler.SupportsStreaming)
            {
                return HandlerLookupResult.NoStreamingSupport(methodName);
            }

            var regularHandler = _handlers.TryGetValue(methodName, out var h) ? h : streamingHandler;
            return HandlerLookupResult.Success(regularHandler, streamingHandler);
        }
        else
        {
            if (!_handlers.TryGetValue(methodName, out var handler))
            {
                return HandlerLookupResult.NotFound(methodName);
            }

            var streamingHandler = _streamingHandlers.TryGetValue(methodName, out var sh) ? sh : null;
            return HandlerLookupResult.Success(handler, streamingHandler);
        }
    }

    private void LogHandlerRegistration()
    {
        _logger.LogInfo($"HandlerRegistry initialized with {_handlers.Count} handlers, {_streamingHandlers.Count} support streaming");
        foreach (var handler in _streamingHandlers)
        {
            _logger.LogDebug($"  - {handler.Key} (Streaming: {handler.Value.SupportsStreaming})");
        }
    }
}