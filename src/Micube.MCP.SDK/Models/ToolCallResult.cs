using System;
using Newtonsoft.Json;

namespace Micube.MCP.SDK.Models;

public class ToolCallResult
{
    [JsonProperty("content")]
    public List<ToolContent> Content { get; set; } = new();
    
    [JsonProperty("isError")]
    public bool IsError { get; set; } = false;

    public static ToolCallResult Success(params string[] messages)
    {
        return new ToolCallResult
        {
            IsError = false,
            Content = messages.Select(m => new ToolContent("text", m)).ToList()
        };
    }

    public static ToolCallResult Fail(string message)
    {
        return new ToolCallResult
        {
            IsError = true,
            Content = new List<ToolContent> { new ToolContent("text", message) }
        };
    }
}
