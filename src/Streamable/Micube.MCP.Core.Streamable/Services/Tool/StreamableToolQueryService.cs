using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Streamable.Dispatcher;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public class StreamableToolQueryService : IToolQueryService
{
    private readonly IStreamableToolDispatcher _dispatcher;

    public StreamableToolQueryService(IStreamableToolDispatcher dispatcher)
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
