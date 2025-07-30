using System.Text.Json;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.SDK.Interfaces;

public interface IMcpToolGroup
{
    /// <summary>
    /// 이 Tool 그룹의 이름
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// 지정된 메서드 이름과 파라미터로 Tool 기능을 실행합니다.
    /// MCP Core는 이 메서드를 호출합니다.
    /// </summary>
    /// <param name="toolName">호출할 기능 이름 (ex. "ReadTag")</param>
    /// <param name="parameters">파라미터 키-값 쌍</param>
    /// <returns>실행 결과 (object or null)</returns>
    Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters);

    /// <summary> ToolGroup 자체 Config 주입 </summary>
    void Configure(JsonElement? config);
}
