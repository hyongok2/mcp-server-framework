using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpPromptMessage
{
    /// <summary>
    /// 메시지 역할 (system, user, assistant)
    /// </summary>
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 메시지 내용
    /// </summary>
    [JsonProperty("content")]
    public McpPromptContent Content { get; set; } = new();
}
