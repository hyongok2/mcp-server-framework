using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Handlers.Resources;

public class ResourcesListHandler : IMethodHandler
{
    private readonly IResourceService _resourceService;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.ResourcesList;
    public bool RequiresInitialization => true;

    public ResourcesListHandler(IResourceService resourceService, IMcpLogger logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("Received request for available resources", message.Id);

        try
        {
            var resources = await _resourceService.GetResourcesAsync();
            _logger.LogInfo($"Found {resources.Count} resources", message.Id);

            foreach (var resource in resources)
            {
                _logger.LogDebug($"Resource: {resource.Name} ({resource.Uri})", message.Id);
            }

            return new McpSuccessResponse
            {
                Id = message.Id,
                Result = new { resources = resources }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get resources: {ex.Message}", message.Id, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Failed to get resources", ex.Message);
        }
    }
}