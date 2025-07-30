using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpToolCallRequest
{

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}

