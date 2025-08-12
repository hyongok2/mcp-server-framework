using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Options;

namespace Micube.MCP.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class McpController : ControllerBase
{
    private readonly IMcpMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly FeatureOptions _features;

    public McpController(IMcpMessageDispatcher dispatcher, IMcpLogger logger, IOptions<FeatureOptions> features)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _features = features.Value;
    }
    /// <summary>
    /// Handles incoming MCP messages.
    /// Endpoint => /mcp
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] McpMessage message)
    {
        // Convert JsonElement fields to proper JSON string for logging
        var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
        _logger.LogInfo($"[HTTP] Received message: {messageJson}");

        if (_features.EnableHttp == false)
        {
            _logger.LogInfo("[HTTP] HTTP interface is disabled via configuration.");
            return StatusCode(503, new
            {
                error = "HTTP interface is currently disabled by configuration."
            });
        }
        
        if (message == null)
        {
            _logger.LogError("[HTTP] Received null message");
            return BadRequest(new
            {
                error = "Invalid request: message cannot be null"
            });
        }

        var result = await _dispatcher.HandleAsync(message);

        if (result == null)
        {
            _logger.LogInfo("[HTTP] Notification processed, no response");
            // 알림의 경우 204 No Content 반환
            return NoContent();
        }

        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        _logger.LogInfo($"[HTTP] Response: {responseJson}");

        return Ok(result);
    }
}