using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IToolDispatcher _toolDispatcher;
    private readonly IResourceService _resourceService;
    private readonly IPromptService _promptService;
    private readonly ISessionState _sessionState;
    private readonly IMcpLogger _logger;

    public HealthController(
        IToolDispatcher toolDispatcher,
        IResourceService resourceService, 
        IPromptService promptService,
        ISessionState sessionState,
        IMcpLogger logger)
    {
        _toolDispatcher = toolDispatcher;
        _resourceService = resourceService;
        _promptService = promptService;
        _sessionState = sessionState;
        _logger = logger;
    }

    /// <summary>
    /// 기본 헬스체크 - 서버 가동 상태만 확인
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "0.1.0"
        });
    }

    /// <summary>
    /// 상세 헬스체크 - 모든 컴포넌트 상태 확인
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var healthData = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "0.1.0",
            components = new
            {
                session = new
                {
                    status = _sessionState.IsInitialized ? "initialized" : "not-initialized",
                    healthy = true
                },
                tools = CheckToolsHealthAsync(),
                resources = await CheckResourcesHealthAsync(),
                prompts = await CheckPromptsHealthAsync()
            }
        };

        var allHealthy = CheckAllComponentsHealthy(healthData.components);
        
        return allHealthy ? Ok(healthData) : StatusCode(503, healthData);
    }

    private object CheckToolsHealthAsync()
    {
        try
        {
            var groups = _toolDispatcher.GetAvailableGroups();
            return new
            {
                status = "healthy",
                toolGroupsCount = groups.Count,
                groups = groups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Tools health check failed: {ex.Message}", ex);
            return new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }
    }

    private async Task<object> CheckResourcesHealthAsync()
    {
        try
        {
            var resources = await _resourceService.GetResourcesAsync();
            return new
            {
                status = "healthy",
                resourcesCount = resources.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Resources health check failed: {ex.Message}", ex);
            return new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }
    }

    private async Task<object> CheckPromptsHealthAsync()
    {
        try
        {
            var prompts = await _promptService.GetPromptsAsync();
            return new
            {
                status = "healthy",
                promptsCount = prompts.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Prompts health check failed: {ex.Message}", ex);
            return new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }
    }

    private static bool CheckAllComponentsHealthy(dynamic components)
    {
        return components.session.healthy &&
               components.tools.status == "healthy" &&
               components.resources.status == "healthy" &&
               components.prompts.status == "healthy";
    }
}