using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Options;
using Newtonsoft.Json;

namespace Micube.MCP.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class McpController : ControllerBase
{
    private readonly IMcpMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly FeatureOptions _features;

    private static readonly JsonSerializerSettings _jsonLogSettings = new JsonSerializerSettings
    {
        StringEscapeHandling = StringEscapeHandling.Default,
        Formatting = Formatting.None
    };

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
        _logger.LogInfo($"[HTTP] Received message: {JsonConvert.SerializeObject(message, _jsonLogSettings)}");

        if (_features.EnableHttp == false)
        {
            _logger.LogInfo("[HTTP] HTTP interface is disabled via configuration.");
            return StatusCode(503, new
            {
                error = "HTTP interface is currently disabled by configuration."
            });
        }

        var result = await _dispatcher.HandleAsync(message);

        _logger.LogInfo($"[HTTP] Response: {JsonConvert.SerializeObject(result, _jsonLogSettings)}");

        return Ok(result);
    }
}