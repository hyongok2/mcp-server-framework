using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpMessage
{
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("id")]
    public object? Id { get; set; }

    [JsonProperty("method")]
    public string? Method { get; set; }

    [JsonProperty("params")]
    public object? Params { get; set; }

    [JsonProperty("result")]
    public object? Result { get; set; }

    [JsonProperty("error")]
    public McpError? Error { get; set; }
}
