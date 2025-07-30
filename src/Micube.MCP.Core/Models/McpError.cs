using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("data")]
    public string? Data { get; set; }
}
