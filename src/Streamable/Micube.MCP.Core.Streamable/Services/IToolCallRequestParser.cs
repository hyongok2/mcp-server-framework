using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Streamable.Services;

public interface IToolCallRequestParser
{
    ToolCallParseResult ParseRequest(McpMessage message);
}

public class ToolCallParseResult
{
    public bool IsSuccess { get; init; }
    public McpToolCallRequest? Request { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorCode { get; init; }

    public static ToolCallParseResult Success(McpToolCallRequest request) =>
        new() { IsSuccess = true, Request = request };

    public static ToolCallParseResult Error(string errorMessage, int errorCode) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}