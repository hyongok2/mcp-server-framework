using Microsoft.AspNetCore.Http;

namespace Micube.MCP.Server.Streamable.Services;

public class HttpStreamingResponseService : IHttpStreamingResponseService
{
    public void ConfigureStreamingHeaders(HttpResponse response)
    {
        response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache, no-transform";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["Access-Control-Allow-Origin"] = "*";
        response.Headers["X-Accel-Buffering"] = "no";
        response.Headers["X-Content-Type-Options"] = "nosniff";
    }
}