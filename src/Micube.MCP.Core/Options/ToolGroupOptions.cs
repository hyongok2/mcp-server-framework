using System;

namespace Micube.MCP.Core.Options;

public class ToolGroupOptions
{
    public string Directory { get; set; } = "tools";
    public List<string> Whitelist { get; set; } = new();
}