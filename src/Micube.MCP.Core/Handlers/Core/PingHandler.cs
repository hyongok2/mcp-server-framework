using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Handlers.Core;

public class PingHandler : IMethodHandler
{
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.Ping;
    public bool RequiresInitialization => true;

    public PingHandler(IMcpLogger logger)
    {
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogDebug("[Ping] Received ping request.");
        
        return await Task.FromResult(new McpSuccessResponse
        {
            Id = message.Id,
            Result = new { }
        });
    }
}
