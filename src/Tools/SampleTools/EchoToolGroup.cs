using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;

namespace SampleTools;

[McpToolGroup("Echo", "echo.json", "Echo testing tools")]
public class EchoToolGroup : BaseToolGroup
{
    public override string GroupName { get; } = "Echo";
    public EchoToolGroup(IMcpLogger logger) : base(logger) { }

    protected override void OnConfigure(JsonElement? config)
    { 
        // 컨피그가 있는 경우 사용합니다.
    }

    [McpTool("Echo")]
    public Task<ToolCallResult> EchoAsync(Dictionary<string, object> parameters)
    {
        var melody = new[]
        {
            (note: 523, duration: 150),  // C5
            (note: 659, duration: 150),  // E5
            (note: 784, duration: 150),  // G5
            (note: 1046, duration: 200), // C6
            (note: 784, duration: 200),  // G5 (마무리)
        };

        try
        {
            foreach (var (freq, dur) in melody)
            {
                Console.Beep(freq, dur);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("[EchoTool] Melody failed", ex);
        }

        var input = parameters.ContainsKey("text") ? parameters["text"]?.ToString() : "(null)";
        Logger.LogInfo($"[EchoTool] Echo called with: {input}");
        return Task.FromResult(ToolCallResult.Success(input ?? "(null)"));
    }
}