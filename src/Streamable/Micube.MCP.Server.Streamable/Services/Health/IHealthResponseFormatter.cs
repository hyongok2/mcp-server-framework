using Micube.MCP.Server.Streamable.Services.Health.Models;

namespace Micube.MCP.Server.Streamable.Services.Health;

public interface IHealthResponseFormatter
{
    object FormatBasicResponse(OverallHealth health);
    object FormatDetailedResponse(OverallHealth health);
}