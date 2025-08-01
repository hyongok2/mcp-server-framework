using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpPromptResponse
{
    /// <summary>
    /// 프롬프트 설명
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 생성된 메시지들
    /// </summary>
    [JsonProperty("messages")]
    public List<McpPromptMessage> Messages { get; set; } = new();
}

