using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.Server.Streamable.Options;
using System.Text;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Server.Streamable.Controllers;

[ApiController]
[Route("[controller]")]
public class McpController : ControllerBase
{
    private readonly IStreamingMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly IOptions<StreamableServerOptions> _options;

    public McpController(
        IStreamingMessageDispatcher dispatcher,
        IMcpLogger logger,
        IOptions<StreamableServerOptions> options)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Unified MCP endpoint - handles both regular and streaming requests
    /// Client always sends identical requests, server decides streaming based ONLY on method capabilities
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleMcp([FromBody] McpMessage message)
    {
        var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
        _logger.LogInfo($"[HTTP] Received message: {messageJson}");
        try
        {
            // Server determines streaming purely based on method capabilities
            // Client request format is always identical - no special headers/params needed
            bool methodSupportsStreaming = _dispatcher.SupportsStreaming(message.Method ?? "");

            if (methodSupportsStreaming)
            {
                // Method supports streaming - use streaming response
                return await HandleStreamingResponse(message);
            }

            // Method doesn't support streaming - use regular JSON response
            return await HandleRegularResponse(message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling MCP request", ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<IActionResult> HandleRegularResponse(McpMessage message)
    {
        var result = await _dispatcher.HandleAsync(message);
        return Ok(result);
    }

    private async Task<IActionResult> HandleStreamingResponse(McpMessage message)
    {
        Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["Access-Control-Allow-Origin"] = "*";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Build cancellation chain with server timeout
        using var timeoutCts = new CancellationTokenSource(_options.Value.StreamTimeout);
        using var requestLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token);
        var serverToken = requestLinkedCts.Token;

        var enableHeartbeat = _options.Value.EnableHeartbeat;
        var heartbeatInterval = _options.Value.HeartbeatInterval;
        using var heartbeatTimer = enableHeartbeat ? new PeriodicTimer(heartbeatInterval) : null;
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);

        try
        {
            // Start heartbeat in background
            Task? heartbeatTask = null;
            if (heartbeatTimer != null)
            {
                heartbeatTask = Task.Run(async () =>
                {
                    try
                    {
                        while (await heartbeatTimer.WaitForNextTickAsync(heartbeatCts.Token))
                        {
                            await Response.WriteAsync(": heartbeat\n\n", heartbeatCts.Token);
                            await Response.Body.FlushAsync(heartbeatCts.Token);
                        }
                    }
                    catch { /* ignore */ }
                }, heartbeatCts.Token);
            }

            var stream = _dispatcher.HandleStreamingAsync(message, serverToken);

            await foreach (var chunk in stream.WithCancellation(serverToken))
            {
                var sseData = FormatAsSSE(message, chunk);
                await Response.WriteAsync(sseData, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);

                if (chunk.IsFinal)
                    break;
            }
            // Stop heartbeat
            heartbeatCts.Cancel();
            if (heartbeatTask != null) await heartbeatTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Streaming request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling streaming request", ex);
            var errorChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = ex.Message,
                IsFinal = true
            };
            await Response.WriteAsync(FormatAsSSE(message, errorChunk));
        }

        return new EmptyResult();
    }

    private static string FormatAsSSE(McpMessage request, StreamChunk chunk)
    {
        // Normalize progress to 0..1 if needed
        double? progress = chunk.Progress;
        if (progress.HasValue && progress.Value > 1.0)
        {
            progress = progress.Value / 100.0;
        }

        var payload = new
        {
            jsonrpc = "2.0",
            method = "mcp/streamChunk",
            @params = new
            {
                id = request.Id,
                sequence = chunk.SequenceNumber,
                chunk = new
                {
                    type = chunk.Type.ToString().ToLowerInvariant(),
                    content = chunk.Content,
                    isFinal = chunk.IsFinal,
                    timestamp = chunk.Timestamp.ToString("O"),
                    progress = progress,
                    metadata = chunk.Metadata
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var eventName = "mcp/streamChunk";
        var eventId = chunk.SequenceNumber.ToString();
        return $"event: {eventName}\n" +
               $"id: {eventId}\n" +
               $"data: {json}\n\n";
    }
}