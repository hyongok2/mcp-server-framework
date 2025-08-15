using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public interface IHttpStreamingResponseService
{
    void ConfigureStreamingHeaders(HttpResponse response);
}