using System.Runtime.CompilerServices;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Streamable.Handlers;

/// <summary>
/// Stream-compatible handler for tools/call that supports both
/// regular JSON-RPC response and MCP 0618-compliant streaming.
/// </summary>
public class ToolsCallStreamHandler : IStreamingHandler
{
    private readonly IStreamableToolDispatcher _streamableToolDispatcher;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.ToolsCall;
    public bool RequiresInitialization => true;
    public bool SupportsStreaming => true;

    public ToolsCallStreamHandler(IStreamableToolDispatcher streamableToolDispatcher, IMcpLogger logger)
    {
        _streamableToolDispatcher = streamableToolDispatcher;
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

        if (message.Params == null)
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Missing params: Tool call requires parameters",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INVALID_PARAMS,
                    ["message"] = "Missing params"
                }
            };
            yield break;
        }

        McpToolCallRequest? call;
        StreamChunk? parseErrorChunk = null;
        try
        {
            call = JsonConvert.DeserializeObject<McpToolCallRequest>(message.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            parseErrorChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = ex.Message,
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INVALID_PARAMS,
                    ["message"] = "Invalid params format"
                }
            };
            call = null;
        }

        if (parseErrorChunk != null)
        {
            yield return parseErrorChunk;
            yield break;
        }

        if (call == null || string.IsNullOrEmpty(call.Name))
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Tool name is required",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INVALID_PARAMS,
                    ["message"] = "Invalid params"
                }
            };
            yield break;
        }

        // Optional: initial metadata chunk
        yield return new StreamChunk
        {
            Type = StreamChunkType.Metadata,
            Content = "tools/call started",
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["tool"] = call.Name,
                ["argumentsPreview"] = string.Join(",", (call.Arguments != null ? call.Arguments.Keys : System.Linq.Enumerable.Empty<string>()))
            }
        };

        // Execute the tool (non-streaming). Cancellation is propagated.
        Micube.MCP.SDK.Models.ToolCallResult? result = null;
        StreamChunk? exceptionChunk = null;
        try
        {
            // TODO: 아래 함수를 변경해야 함. 스트리밍이 가능한 방식으로. 
            result = await _streamableToolDispatcher.InvokeAsync(call.Name, call.Arguments ?? new Dictionary<string, object>(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo($"tools/call canceled: {requestId}");
            exceptionChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = "Request canceled",
                IsFinal = true,
                SequenceNumber = 2,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INTERNAL_ERROR,
                    ["message"] = "Canceled"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during streaming tool invocation: {ex.Message}", ex);
            exceptionChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = ex.Message,
                IsFinal = true,
                SequenceNumber = 2,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INTERNAL_ERROR,
                    ["message"] = "Tool execution failed"
                }
            };
        }

        if (exceptionChunk != null)
        {
            yield return exceptionChunk;
            yield break;
        }

        if (result == null || result.IsError)
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = result?.Content.FirstOrDefault()?.Text ?? "Tool execution failed",
                IsFinal = true,
                SequenceNumber = 2,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["code"] = (int)McpErrorCodes.INTERNAL_ERROR,
                    ["message"] = "Tool execution failed",
                    ["tool"] = call.Name
                }
            };
            yield break;
        }

        // Final completion chunk with result payload per MCP 0618 guidance
        yield return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = "Tool execution completed",
            IsFinal = true,
            SequenceNumber = 3,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["response"] = new
                {
                    jsonrpc = "2.0",
                    id = message.Id,
                    result = result
                }
            }
        };
    }
}
