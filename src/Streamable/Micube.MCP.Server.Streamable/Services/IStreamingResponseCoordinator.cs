using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Server.Streamable.Services;

public interface IStreamingResponseCoordinator
{
    Task<IActionResult> HandleStreamingResponseAsync(McpMessage message, HttpContext httpContext);
}