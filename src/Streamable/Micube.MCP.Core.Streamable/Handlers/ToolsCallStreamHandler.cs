using System.Runtime.CompilerServices;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Services.Handler;
using Micube.MCP.Core.Streamable.Services.Tool;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Handlers;

/// <summary>
/// Stream-compatible handler for tools/call that supports both
/// regular JSON-RPC response and MCP 0618-compliant streaming.
/// </summary>
public class ToolsCallStreamHandler : IStreamingHandler
{
    private readonly IToolCallRequestParser _requestParser;
    private readonly IHandlerErrorChunkFactory _errorChunkFactory;
    private readonly IToolCallStreamProcessor _streamProcessor;
    private readonly IMcpLogger _logger;

    public string MethodName => "tools/call";
    public bool RequiresInitialization => true;
    public bool SupportsStreaming => true;

    public ToolsCallStreamHandler(
        IToolCallRequestParser requestParser,
        IHandlerErrorChunkFactory errorChunkFactory,
        IToolCallStreamProcessor streamProcessor,
        IMcpLogger logger)
    {
        _requestParser = requestParser;
        _errorChunkFactory = errorChunkFactory;
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Backward-compatible non-streaming execution path.
    /// </summary>
    public Task<object?> HandleAsync(McpMessage message)
    {
        throw new NotSupportedException("Use HandleStreamingAsync for tools/call to support streaming.");
    }

    /// <summary>
    /// Streaming execution that wraps the final tool result into
    /// MCP 0618-compliant streaming chunks. If tools are not
    /// inherently streaming, this still emits a final 'complete' chunk
    /// for compatibility.
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> HandleStreamingAsync(
        McpMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = message.Id?.ToString() ?? "unknown";

        // Parse and validate the request
        var parseResult = _requestParser.ParseRequest(message);
        if (!parseResult.IsSuccess)
        {
            yield return _errorChunkFactory.CreateValidationErrorChunk(
                parseResult.ErrorMessage!, 
                parseResult.ErrorCode!.Value);
            yield break;
        }

        _logger.LogDebug($"Processing tool call: {parseResult.Request!.Name}");

        // Process the tool call stream
        await foreach (var chunk in _streamProcessor.ProcessToolCallStreamAsync(
            parseResult.Request!, 
            message.Id!, 
            cancellationToken))
        {
            yield return chunk;
        }
    }
}
