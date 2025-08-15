using System;
using System.Text.Json;

namespace Micube.MCP.SDK.Streamable.Interface;

public interface IStreamableMcpToolGroup
{
    /// <summary>
    /// 이 Tool 그룹의 이름
    /// </summary>
    string GroupName { get; }

    // TODO: 아래 함수를 변경해야 함. 스트리밍이 가능한 방식으로. 
    Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary> ToolGroup 자체 Config 주입 </summary>
    void Configure(JsonElement? config);
}
