using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Streamable.Services;

namespace Micube.MCP.Server.Streamable.Controllers;

[ApiController]
[Route("[controller]")]
public class McpController : ControllerBase
{
    private readonly IStreamingMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly IStreamingResponseCoordinator _streamingCoordinator;

    public McpController(
        IStreamingMessageDispatcher dispatcher,
        IMcpLogger logger,
        IStreamingResponseCoordinator streamingCoordinator)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _streamingCoordinator = streamingCoordinator;
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
                return await _streamingCoordinator.HandleStreamingResponseAsync(message, HttpContext);
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

}