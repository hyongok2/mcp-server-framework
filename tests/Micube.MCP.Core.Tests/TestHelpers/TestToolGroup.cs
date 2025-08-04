using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Tests.TestHelpers;

[McpToolGroup("TestGroup", "test.json", "Test tool group")]
public class TestToolGroup : BaseToolGroup
{
    public override string GroupName => "TestGroup";

    public TestToolGroup(IMcpLogger logger) : base(logger)
    {
    }

    protected override void OnConfigure(JsonElement? config)
    {
        // Test configuration
    }

    [McpTool("TestTool")]
    public async Task<ToolCallResult> TestToolAsync(Dictionary<string, object> parameters)
    {
        var message = parameters.TryGetValue("message", out var value) ? value?.ToString() : "Hello";
        return await Task.FromResult(ToolCallResult.Success($"Test: {message}"));
    }

    [McpTool("ErrorTool")]
    public async Task<ToolCallResult> ErrorToolAsync(Dictionary<string, object> parameters)
    {
        return await Task.FromResult(ToolCallResult.Fail("Test error"));
    }

    [McpTool("ThrowTool")]
    public async Task<ToolCallResult> ThrowToolAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
    }
}