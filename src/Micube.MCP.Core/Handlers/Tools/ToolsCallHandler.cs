using System;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Handlers.Tools;

public class ToolsCallHandler : IMethodHandler
{
    private readonly IToolDispatcher _toolDispatcher;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.ToolsCall;
    public bool RequiresInitialization => true;

    public ToolsCallHandler(IToolDispatcher toolDispatcher, IMcpLogger logger)
    {
        _toolDispatcher = toolDispatcher;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message.Params == null)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS, 
                "Missing params", "Tool call requires parameters");
        }

        McpToolCallRequest? call;
        try
        {
            call = JsonConvert.DeserializeObject<McpToolCallRequest>(message.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS, 
                "Invalid params format", ex.Message);
        }

        if (call == null || string.IsNullOrEmpty(call.Name))
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS, 
                "Invalid params", "Tool name is required");
        }

        try
        {
            var result = await _toolDispatcher.InvokeAsync(call.Name, call.Arguments);

            if (result.IsError)
            {
                return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR, 
                    "Tool execution failed", result.Content.FirstOrDefault()?.Text ?? "Unknown error");
            }

            return new McpSuccessResponse
            {
                Id = message.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during tool invocation: {ex.Message}", ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR, 
                "Tool execution failed", ex.Message);
        }
    }
}