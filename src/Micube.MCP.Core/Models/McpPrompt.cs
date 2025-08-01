using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpPrompt
{
    /// <summary>
    /// 프롬프트의 고유 식별자
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 프롬프트에 대한 설명 (optional)
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 프롬프트 매개변수들 (optional)
    /// </summary>
    [JsonProperty("arguments")]
    public List<McpPromptArgument>? Arguments { get; set; }
}
