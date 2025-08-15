using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public interface IHeartbeatService
{
    Task StartHeartbeatAsync(HttpResponse response, TimeSpan interval, CancellationToken cancellationToken);
}