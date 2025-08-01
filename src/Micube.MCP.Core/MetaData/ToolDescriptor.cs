using System;

namespace Micube.MCP.Core.MetaData;

public class ToolDescriptor
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = "";
    public List<ParameterDescriptor> Parameters { get; set; } = new();
    public bool StructuredOutput { get; set; } = false;
    public object? OutputSchema { get; set; }
}