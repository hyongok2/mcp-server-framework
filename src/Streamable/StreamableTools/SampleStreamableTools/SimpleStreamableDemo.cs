using System.Text.Json;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Abstracts;

namespace SampleStreamableTools;

[McpToolGroup("SimpleStreamableDemo", "simple-streamable-demo.json", "Simple demonstration of streamable MCP tools")]
public class SimpleStreamableDemoToolGroup : BaseStreamableToolGroup
{
    public override string GroupName { get; } = "SimpleStreamableDemo";

    public SimpleStreamableDemoToolGroup(IMcpLogger logger) : base(logger)
    {
    }

    protected override void OnConfigure(JsonElement? config)
    {
        LogStreamOperation("Configure", "SimpleStreamableDemo tool group initialized");
    }

    /// <summary>
    /// Regular (non-streaming) echo tool for compatibility
    /// </summary>
    [McpTool("simple_echo")]
    public Task<ToolCallResult> SimpleEchoAsync(Dictionary<string, object> parameters)
    {
        var input = parameters.ContainsKey("text") ? parameters["text"]?.ToString() : "Hello Streamable World!";
        Logger.LogInfo($"[SimpleEchoTool] Echo called with: {input}");
        
        var result = new { 
            echo = input, 
            timestamp = DateTime.UtcNow,
            toolType = "streamable-compatible"
        };
        
        return Task.FromResult(ToolCallResult.Success(JsonSerializer.Serialize(result)));
    }

    /// <summary>
    /// Stream counting numbers - simplified version
    /// </summary>
    [McpTool("simple_count")]
    public Task<ToolCallResult> SimpleCountAsync(Dictionary<string, object> parameters)
    {
        var maxCount = parameters.ContainsKey("count") && int.TryParse(parameters["count"]?.ToString(), out var c) ? c : 5;
        
        var results = new List<object>();
        for (int i = 1; i <= maxCount; i++)
        {
            results.Add(new { number = i, timestamp = DateTime.UtcNow });
        }
        
        Logger.LogInfo($"[SimpleCountTool] Generated {maxCount} numbers");
        return Task.FromResult(ToolCallResult.Success(JsonSerializer.Serialize(results)));
    }
}