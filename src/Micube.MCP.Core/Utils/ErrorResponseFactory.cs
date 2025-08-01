using System;
using Micube.MCP.Core.Models;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Utils;

public static class ErrorResponseFactory
{
    public static McpErrorResponse Create(object? id, int code, string message, object? data = null)
    {
        return new McpErrorResponse
        {
            Id = id ?? 0,
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data != null ? JsonConvert.SerializeObject(data, Formatting.Indented) : null
            }
        };
    }
}