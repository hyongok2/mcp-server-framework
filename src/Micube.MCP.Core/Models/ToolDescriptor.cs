using System;

namespace Micube.MCP.Core.Models;

public class ToolDescriptor
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = "";
    public List<ParameterDescriptor> Parameters { get; set; } = new();
}