using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;

namespace Micube.MCP.Core.MetaData;

public static class ToolMetadataHelper
{
    public static List<McpToolInfo> ConvertToToolInfo(ToolGroupMetadata metadata)
    {
        return metadata.Tools.Select(t => new McpToolInfo
        {
            Name = $"{metadata.GroupName}{ToolConstants.NameSeparator}{t.Name}",
            Description = t.Description,
            InputSchema = ConvertToJsonSchema(t)
        }).ToList();    
    }

    public static object ConvertToJsonSchema(ToolDescriptor descriptor)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in descriptor.Parameters)
        {
            properties[param.Name] = new
            {
                type = param.Type,
                description = param.Description
            };
            if (param.Required)
                required.Add(param.Name);
        }

        return new
        {
            type = "object",
            properties,
            required
        };
    }
}