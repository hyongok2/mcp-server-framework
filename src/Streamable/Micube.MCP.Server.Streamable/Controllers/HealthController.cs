using System;
using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Server.Streamable.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IStreamableToolDispatcher _toolDispatcher;
    private readonly ISessionState _sessionState;
    private readonly IMcpLogger _logger;

    public HealthController(
        IStreamableToolDispatcher toolDispatcher,
        ISessionState sessionState,
        IMcpLogger logger)
    {
        _toolDispatcher = toolDispatcher;
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
    public IActionResult GetDetailed()
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

    private static bool CheckAllComponentsHealthy(dynamic components)
    {
        return components.session.healthy &&
               components.tools.status == "healthy" ;
    }
}
