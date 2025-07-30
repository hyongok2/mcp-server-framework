using System;
using Newtonsoft.Json;

namespace Micube.MCP.SDK.Models;

public class ToolContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "text"; // text, image, code 등
    [JsonProperty("text")]
    public string? Text { get; set; }

    public ToolContent() { }

    public ToolContent(string type, string? text)
    {
        Type = type;
        Text = text;
    }
}