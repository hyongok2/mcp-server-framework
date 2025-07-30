using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Models;

namespace Micube.MCP.Core.Dispatcher;

public interface IToolDispatcher
{
    Task<ToolCallResult> InvokeAsync(string fullToolName, Dictionary<string, object> parameters);
    List<string> GetAvailableGroups();
    ToolGroupMetadata? GetGroupMetadata(string groupName);
}
