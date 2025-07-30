using System;
using System.Text.Json;

namespace Micube.MCP.Core.Models;

public class ToolGroupMetadata
{
    public string GroupName { get; set; } = default!;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public string? Author { get; set; }
    public List<ToolDescriptor> Tools { get; set; } = new();
    
    public JsonElement? Config { get; set; } // Generic Config 저장
}