using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;
using Micube.MCP.Server.Streamable.Options;

namespace Micube.MCP.Server.Streamable.Services;

public class StreamingResponseCoordinator : IStreamingResponseCoordinator
{
    private readonly IStreamingMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly IHttpStreamingResponseService _responseService;
    private readonly ISseFormatter _sseFormatter;
    private readonly IHeartbeatService _heartbeatService;
    private readonly ICancellationTokenBuilder _cancellationTokenBuilder;
    private readonly IOptions<StreamableServerOptions> _options;

    public StreamingResponseCoordinator(
        IStreamingMessageDispatcher dispatcher,
        IMcpLogger logger,
        IHttpStreamingResponseService responseService,
        ISseFormatter sseFormatter,
        IHeartbeatService heartbeatService,
        ICancellationTokenBuilder cancellationTokenBuilder,
        IOptions<StreamableServerOptions> options)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _responseService = responseService;
        _sseFormatter = sseFormatter;
        _heartbeatService = heartbeatService;
        _cancellationTokenBuilder = cancellationTokenBuilder;
        _options = options;
    }

    public async Task<IActionResult> HandleStreamingResponseAsync(McpMessage message, HttpContext httpContext)
    {
        _responseService.ConfigureStreamingHeaders(httpContext.Response);

        using var tokenContext = _cancellationTokenBuilder.CreateStreamingCancellationTokens(
            httpContext, _options.Value.StreamTimeout);

        try
        {
            Task? heartbeatTask = null;
            if (_options.Value.EnableHeartbeat)
            {
                heartbeatTask = _heartbeatService.StartHeartbeatAsync(
                    httpContext.Response, 
                    _options.Value.HeartbeatInterval, 
                    tokenContext.HeartbeatToken);
            }

            var stream = _dispatcher.HandleStreamingAsync(message, tokenContext.ServerToken);

            await foreach (var chunk in stream.WithCancellation(tokenContext.ServerToken))
            {
                var sseData = _sseFormatter.FormatAsSSE(message, chunk);
                await httpContext.Response.WriteAsync(sseData, httpContext.RequestAborted);
                await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);

                if (chunk.IsFinal)
                    break;
            }

            tokenContext.CancelHeartbeat();
            if (heartbeatTask != null) 
                await heartbeatTask;
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
            await httpContext.Response.WriteAsync(_sseFormatter.FormatAsSSE(message, errorChunk));
        }

        return new EmptyResult();
    }
}