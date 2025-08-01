using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpResourceReadRequest
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;
}