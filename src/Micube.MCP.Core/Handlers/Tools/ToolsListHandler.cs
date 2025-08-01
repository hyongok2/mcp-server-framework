using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Handlers.Tools;

public class ToolsListHandler : IMethodHandler
{
    private readonly IToolQueryService _toolQuery;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.ToolsList;
    public bool RequiresInitialization => true;

    public ToolsListHandler(IToolQueryService toolQuery, IMcpLogger logger)
    {
        _toolQuery = toolQuery;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("[Tool List] Received request for available tools.");

        var tools = _toolQuery.GetAllTools();
        _logger.LogInfo($"[Tool List] Found {tools.Count} tools.");

        foreach (var tool in tools)
        {
            _logger.LogDebug($"Tool: {tool.Name}, Description: {tool.Description}");
        }

        return await Task.FromResult(new McpSuccessResponse
        {
            Id = message.Id,
            Result = new { tools = tools }
        });
    }
}

