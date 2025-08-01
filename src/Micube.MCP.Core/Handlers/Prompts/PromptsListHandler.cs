using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Handlers.Prompts;

public class PromptsListHandler : IMethodHandler
{
    private readonly IPromptService _promptService;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.PromptsList;
    public bool RequiresInitialization => true;

    public PromptsListHandler(IPromptService promptService, IMcpLogger logger)
    {
        _promptService = promptService;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("Received request for available prompts", message.Id);

        try
        {
            var prompts = await _promptService.GetPromptsAsync();
            _logger.LogInfo($"Found {prompts.Count} prompts", message.Id);

            foreach (var prompt in prompts)
            {
                _logger.LogDebug($"Prompt: {prompt.Name} - {prompt.Description}", message.Id);
            }

            return new McpSuccessResponse
            {
                Id = message.Id,
                Result = new { prompts = prompts }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get prompts: {ex.Message}", message.Id, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Failed to get prompts", ex.Message);
        }
    }
}

