using System;
using System.Text.Json;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Server.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMcpLogger _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, IMcpLogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception: {ex.Message}", ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        var response = new
        {
            error = new
            {
                code = -32603, // JSON-RPC Internal Error
                message = "Internal server error",
                data = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                    ? exception.ToString() 
                    : "An unexpected error occurred"
            },
            jsonrpc = "2.0",
            id = context.Items["RequestId"] ?? null
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

