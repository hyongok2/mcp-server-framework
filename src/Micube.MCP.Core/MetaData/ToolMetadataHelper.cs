using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.MetaData;

public static class ToolMetadataHelper
{
    public static List<McpToolInfo> ConvertToToolInfo(ToolGroupMetadata metadata)
    {
        return metadata.Tools.Select(t => new McpToolInfo
        {
            Name = $"{metadata.GroupName}.{t.Name}",
            Description = t.Description,
            InputSchema = t.Parameters
        }).ToList();
    }
}