using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.SDK.Tests.Abstracts;

public class TestableToolGroup : BaseToolGroup
{
    public override string GroupName => "TestableGroup";
    public bool ConfigureCalled { get; private set; }
    public JsonElement? LastConfig { get; private set; }

    public TestableToolGroup(IMcpLogger logger) : base(logger) { }

    protected override void OnConfigure(JsonElement? config)
    {
        ConfigureCalled = true;
        LastConfig = config;
    }

    [McpTool("StringTool")]
    public async Task<string> StringToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return parameters.TryGetValue("input", out var value) ? value?.ToString() ?? "null" : "default";
    }

    [McpTool("IntTool")]
    public async Task<int> IntToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return 42;
    }

    [McpTool("BoolTool")]
    public async Task<bool> BoolToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return true;
    }

    [McpTool("ObjectTool")]
    public async Task<object> ObjectToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return new { name = "test", value = 123 };
    }

    [McpTool("VoidTool")]
    public async Task VoidToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        // Void return
    }

    [McpTool("ToolCallResultTool")]
    public async Task<ToolCallResult> ToolCallResultToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return ToolCallResult.Success("Direct result");
    }

    [McpTool("ThrowingTool")]
    public async Task<string> ThrowingToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
    }

    [McpTool("NullTool")]
    public async Task<string?> NullToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        return null;
    }
}