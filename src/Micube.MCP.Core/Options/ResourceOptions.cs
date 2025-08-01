using System;

namespace Micube.MCP.Core.Options;

public class ResourceOptions
{
    public string Directory { get; set; } = "docs";
    public string MetadataFileName { get; set; } = ".mcp-resources.json";
    public List<string> SupportedExtensions { get; set; } = new()
    {
        ".md", ".txt", ".json", ".yaml", ".yml", ".xml"
    };
}