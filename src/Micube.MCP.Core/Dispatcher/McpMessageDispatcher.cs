using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Dispatcher;

public class McpMessageDispatcher : IMcpMessageDispatcher
{
    private readonly IToolDispatcher _toolDispatcher;
    private readonly IToolQueryService _toolQuery;
    private readonly IMcpLogger _logger;
    private bool _isInitialized = false;

    public McpMessageDispatcher(IToolDispatcher toolDispatcher, IToolQueryService toolQueryService, IMcpLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolDispatcher = toolDispatcher;
        _toolQuery = toolQueryService;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message == null)
        {
            return CreateErrorResponse(null, McpErrorCodes.PARSE_ERROR, "Invalid JSON", "Failed to parse JSON-RPC message");
        }

        if (message.JsonRpc != "2.0")
        {
            return CreateErrorResponse(message.Id, McpErrorCodes.INVALID_REQUEST, "Invalid JSON-RPC version", "Only version 2.0 is supported");
        }

        if (string.IsNullOrEmpty(message.Method))
        {
            return CreateErrorResponse(message.Id, McpErrorCodes.INVALID_REQUEST, "Missing method", "Method field is required");
        }

        try
        {
            return message.Method switch
            {
                "initialize" => await HandleInitializeAsync(message),
                "ping" => HandlePing(message),
                "tools/list" => HandleToolList(message),
                "tools/call" => await HandleToolInvoke(message),
                "notifications/initialized" => HandleInitializedNotification(message),
                _ => CreateErrorResponse(message.Id, McpErrorCodes.METHOD_NOT_FOUND, "Method not found", message.Method)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception in method '{message.Method}': {ex.Message}", ex);
            return CreateErrorResponse(message.Id, McpErrorCodes.INTERNAL_ERROR, "Internal error", ex.Message);
        }
    }

    private async Task<McpSuccessResponse> HandleInitializeAsync(McpMessage message)
    {
        _logger.LogInfo("[Initialize] Received MCP Server Initialize Request.");

        // 클라이언트 capabilities 검증 (선택적)
        if (message.Params != null)
        {
            var clientInfo = JsonConvert.DeserializeObject<dynamic>(message.Params.ToString() ?? "{}");
            _logger.LogInfo($"[Initialize] Client info: {JsonConvert.SerializeObject(clientInfo)}");
        }

        _isInitialized = true;

        return await Task.FromResult(new McpSuccessResponse
        {
            Id = message.Id,
            Result = new
            {
                protocolVersion = "2025-06-18", // ✅ 최신 버전으로 업데이트
                serverInfo = new
                {
                    name = "Micube MCP Server Framework",
                    version = "0.1.0",
                    description = "A modular and extensible tool execution framework."
                },
                capabilities = new
                {
                    tools = new { },
                    logging = new { } // 로깅 지원 명시
                }
            }
        });
    }

    private McpSuccessResponse HandlePing(McpMessage message)
    {
        _logger.LogDebug("[Ping] Received ping request.");

        return new McpSuccessResponse
        {
            Id = message.Id,
            Result = new { } // 빈 객체 반환
        };
    }

    private object? HandleInitializedNotification(McpMessage message)
    {
        _logger.LogInfo("[Initialized] Client initialization completed.");

        // JSON-RPC 알림이므로 응답하지 않음
        return null; // null 반환으로 "응답 없음" 표시
    }

    private McpSuccessResponse HandleToolList(McpMessage request)
    {
        if (!_isInitialized)
        {
            _logger.LogError("[Tool List] Request received before initialization.");
            return new McpSuccessResponse
            {
                Id = request.Id,
                Result = new { tools = new object[0] }
            };
        }

        _logger.LogInfo("[Tool List] Received request for available tools.");

        var tools = _toolQuery.GetAllTools();

        _logger.LogInfo($"[Tool List] Found {tools.Count} tools.");

        foreach (var tool in tools)
        {
            _logger.LogDebug($"Tool: {tool.Name}, Description: {tool.Description}");
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
        if (!_isInitialized)
        {
            return CreateErrorResponse(request.Id, McpErrorCodes.INVALID_REQUEST,
                "Server not initialized", "Call initialize first");
        }

        if (request.Params == null)
        {
            return CreateErrorResponse(request.Id, McpErrorCodes.INVALID_PARAMS,
                "Missing params", "Tool call requires parameters");
        }

        McpToolCallRequest? call;
        try
        {
            call = JsonConvert.DeserializeObject<McpToolCallRequest>(request.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            return CreateErrorResponse(request.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params format", ex.Message);
        }

        if (call == null || string.IsNullOrEmpty(call.Name))
        {
            return CreateErrorResponse(request.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params", "Tool name is required");
        }

        try
        {
            var result = await _toolDispatcher.InvokeAsync(call.Name, call.Arguments);

            if (result.IsError)
            {
                return CreateErrorResponse(request.Id, McpErrorCodes.INTERNAL_ERROR,
                    "Tool execution failed", result.Content.FirstOrDefault()?.Text ?? "Unknown error");
            }

            return new McpSuccessResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during tool invocation: {ex.Message}", ex);
            return CreateErrorResponse(request.Id, McpErrorCodes.INTERNAL_ERROR,
                "Tool execution failed", ex.Message);
        }
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
