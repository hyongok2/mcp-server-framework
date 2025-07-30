using System;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Services;

public class ToolQueryService : IToolQueryService
{
    private readonly IToolDispatcher _dispatcher;

    public ToolQueryService(IToolDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public List<McpToolInfo> GetAllTools()
    {
        return _dispatcher
            .GetAvailableGroups()
            .SelectMany(group =>
            {
                var meta = _dispatcher.GetGroupMetadata(group);
                return meta == null
                    ? Enumerable.Empty<McpToolInfo>()
                    : ToolMetadataHelper.ConvertToToolInfo(meta);
            })
            .ToList();
    }
}