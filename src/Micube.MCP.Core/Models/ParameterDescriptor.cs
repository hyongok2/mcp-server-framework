using System;

namespace Micube.MCP.Core.Models;

public class ParameterDescriptor
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = "string"; // string, int, bool 등
    public bool Required { get; set; } = false;
    public string Description { get; set; } = "";
}