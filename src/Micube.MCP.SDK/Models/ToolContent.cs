using System;

namespace Micube.MCP.SDK.Models;

public class ToolContent
{
    public string Type { get; set; } = "text"; // text, image, code 등
    public string? Text { get; set; }

    public ToolContent() { }

    public ToolContent(string type, string? text)
    {
        Type = type;
        Text = text;
    }
}