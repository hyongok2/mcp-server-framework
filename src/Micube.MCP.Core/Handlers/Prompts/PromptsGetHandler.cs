using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Handlers.Prompts;

public class PromptsGetHandler : IMethodHandler
{
    private readonly IPromptService _promptService;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.PromptsGet;
    public bool RequiresInitialization => true;

    public PromptsGetHandler(IPromptService promptService, IMcpLogger logger)
    {
        _promptService = promptService;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message.Params == null)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Missing params", "Prompt get requires parameters");
        }

        McpPromptGetRequest? request;
        try
        {
            request = JsonConvert.DeserializeObject<McpPromptGetRequest>(message.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params format", ex.Message);
        }

        if (request == null || string.IsNullOrEmpty(request.Name))
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params", "Prompt name is required");
        }

        _logger.LogInfo($"Getting prompt: {request.Name}", message.Id);

        try
        {
            var prompt = await _promptService.GetPromptAsync(request.Name, request.Arguments);

            if (prompt == null)
            {
                return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                    "Prompt not found", $"Prompt '{request.Name}' does not exist");
            }

            _logger.LogInfo($"Successfully retrieved prompt: {request.Name}", message.Id);

            return new McpSuccessResponse
            {
                Id = message.Id,
                Result = prompt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get prompt '{request.Name}': {ex.Message}", message.Id, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Failed to get prompt", ex.Message);
        }
    }
}