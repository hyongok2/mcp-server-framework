using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Handlers.Resources;

public class ResourcesReadHandler : IMethodHandler
{
    private readonly IResourceService _resourceService;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.ResourcesRead;
    public bool RequiresInitialization => true;

    public ResourcesReadHandler(IResourceService resourceService, IMcpLogger logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message.Params == null)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Missing params", "Resource read requires parameters");
        }

        McpResourceReadRequest? request;
        try
        {
            request = JsonConvert.DeserializeObject<McpResourceReadRequest>(message.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params format", ex.Message);
        }

        if (request == null || string.IsNullOrEmpty(request.Uri))
        {
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                "Invalid params", "Resource URI is required");
        }

        _logger.LogInfo($"Reading resource: {request.Uri}", message.Id);

        try
        {
            var content = await _resourceService.ReadResourceAsync(request.Uri);

            if (content == null)
            {
                return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS,
                    "Resource not found", $"Resource '{request.Uri}' does not exist");
            }

            _logger.LogInfo($"Successfully read resource: {request.Uri}", message.Id);

            return new McpSuccessResponse
            {
                Id = message.Id,
                Result = new { contents = new[] { content } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read resource '{request.Uri}': {ex.Message}", message.Id, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Failed to read resource", ex.Message);
        }
    }
}
