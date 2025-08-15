using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Streamable.Dispatcher;

public interface IStreamableToolDispatcher
{

    // TODO: 아래 함수를 변경해야 함. 스트리밍이 가능한 방식으로. 
    Task<ToolCallResult> InvokeAsync(string fullToolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    List<string> GetAvailableGroups();
    ToolGroupMetadata? GetGroupMetadata(string groupName);
}
