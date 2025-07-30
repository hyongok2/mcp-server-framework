using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Dispatcher;

public class McpMessageDispatcher : IMcpMessageDispatcher
{
    private readonly IToolDispatcher _toolDispatcher;
    private readonly IToolQueryService _toolQuery;
    private readonly IMcpLogger _logger;

    public McpMessageDispatcher(IToolDispatcher toolDispatcher, IToolQueryService toolQueryService, IMcpLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolDispatcher = toolDispatcher;
        _toolQuery = toolQueryService;
    }

    public async Task<object> HandleAsync(McpMessage message)
    {
        try
        {
            return message.Method switch
            {
                "initialize" => await HandleInitializeAsync(message),
                "tool/list" => HandleToolList(message),
                "tool/invoke" => await HandleToolInvoke(message),
                _ => CreateErrorResponse(message.Id, -32601, "Method not found", message.Method)
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(message.Id, -32603, "Internal error", ex.Message);
        }
    }

    private async Task<McpSuccessResponse> HandleInitializeAsync(McpMessage message)
    {
        _logger.LogInfo("[Initialize] Received MCP Server Initialize Request.");

        return await Task.FromResult(new McpSuccessResponse
        {
            Id = message.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new
                {
                    name = "Micube MCP Server Framework",
                    version = "1.0.0",
                    description = "A modular and extensible tool execution framework."
                },
                capabilities = new
                {
                    tools = new { }
                }
            }
        });
    }

    private McpSuccessResponse HandleToolList(McpMessage request)
    {
        _logger.LogInfo("[Tool List] Received request for available tools.");

        var tools = _toolQuery.GetAllTools();

        _logger.LogInfo($"[Tool List] Found {tools.Count} tools.");

        foreach (var tool in tools)
        {
            _logger.LogInfo($"Tool: {tool.Name}, Description: {tool.Description}");
        }

        return new McpSuccessResponse
        {
            Id = request.Id,
            Result = new
            {
                tools = tools
            }
        };
    }

    private async Task<object> HandleToolInvoke(McpMessage request)
    {
        var call = JsonConvert.DeserializeObject<McpToolCallRequest>(request.Params?.ToString() ?? "{}");

        if (call == null || string.IsNullOrEmpty(call.Name))
        {
            return CreateErrorResponse(request.Id, -32602, "Invalid params", "Tool name is required");
        }

        var result = await _toolDispatcher.InvokeAsync(call.Name, call.Arguments);

        if (result.IsError)
        {
            return CreateErrorResponse(request.Id, -32603, "Tool execution failed", result.Content);
        }

        return new McpSuccessResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    private McpErrorResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
    {
        string dataString = data != null ? JsonConvert.SerializeObject(data, Formatting.Indented) : string.Empty;

        _logger.LogError($"Error Response: ID={id}, Code={code}, Message={message}, Data={dataString}");

        return new McpErrorResponse
        {
            Id = id ?? 0, // null인 경우 0으로 설정
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = dataString
            }
        };
    }

}
