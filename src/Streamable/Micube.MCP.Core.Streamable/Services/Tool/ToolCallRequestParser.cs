using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Models;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public class ToolCallRequestParser : IToolCallRequestParser
{
    public ToolCallParseResult ParseRequest(McpMessage message)
    {
        if (message.Params == null)
        {
            return ToolCallParseResult.Error(
                "Missing params: Tool call requires parameters",
                (int)McpErrorCodes.INVALID_PARAMS);
        }

        McpToolCallRequest? call;
        try
        {
            call = JsonConvert.DeserializeObject<McpToolCallRequest>(message.Params.ToString() ?? "{}");
        }
        catch (JsonException ex)
        {
            return ToolCallParseResult.Error(
                ex.Message,
                (int)McpErrorCodes.INVALID_PARAMS);
        }

        if (call == null || string.IsNullOrEmpty(call.Name))
        {
            return ToolCallParseResult.Error(
                "Tool name is required",
                (int)McpErrorCodes.INVALID_PARAMS);
        }

        return ToolCallParseResult.Success(call);
    }
}