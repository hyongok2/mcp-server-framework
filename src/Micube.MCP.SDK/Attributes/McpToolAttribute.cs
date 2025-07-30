using System;

namespace Micube.MCP.SDK.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    public string Name { get; }

    public McpToolAttribute(string name)
    {
        Name = name;
    }
}
