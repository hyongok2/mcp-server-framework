using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Streamable.Handlers;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public interface IHandlerRegistry
{
    bool SupportsStreaming(string methodName);
    IMethodHandler? GetHandler(string methodName);
    IStreamingHandler? GetStreamingHandler(string methodName);
    HandlerLookupResult FindHandler(string methodName, bool requiresStreaming = false);
}

public class HandlerLookupResult
{
    public bool Found { get; init; }
    public IMethodHandler? Handler { get; init; }
    public IStreamingHandler? StreamingHandler { get; init; }
    public bool SupportsStreaming { get; init; }
    public string? ErrorMessage { get; init; }

    public static HandlerLookupResult Success(IMethodHandler handler, IStreamingHandler? streamingHandler = null) =>
        new() 
        { 
            Found = true, 
            Handler = handler, 
            StreamingHandler = streamingHandler,
            SupportsStreaming = streamingHandler?.SupportsStreaming ?? false 
        };

    public static HandlerLookupResult NotFound(string methodName) =>
        new() { Found = false, ErrorMessage = $"Method not found: {methodName}" };

    public static HandlerLookupResult NoStreamingSupport(string methodName) =>
        new() { Found = false, ErrorMessage = $"Method '{methodName}' does not support streaming" };
}