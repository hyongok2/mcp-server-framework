using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Server.Streamable.Services.Health;

namespace Micube.MCP.Server.Streamable.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthAggregator _healthAggregator;
    private readonly IHealthResponseFormatter _responseFormatter;

    public HealthController(
        IHealthAggregator healthAggregator,
        IHealthResponseFormatter responseFormatter)
    {
        _healthAggregator = healthAggregator;
        _responseFormatter = responseFormatter;
    }

    /// <summary>
    /// 기본 헬스체크 - 서버 가동 상태만 확인
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = await _healthAggregator.GetBasicHealthAsync();
        var response = _responseFormatter.FormatBasicResponse(health);
        return Ok(response);
    }

    /// <summary>
    /// 상세 헬스체크 - 모든 컴포넌트 상태 확인
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var health = await _healthAggregator.GetDetailedHealthAsync();
        var response = _responseFormatter.FormatDetailedResponse(health);
        
        return health.IsHealthy ? Ok(response) : StatusCode(503, response);
    }
}
