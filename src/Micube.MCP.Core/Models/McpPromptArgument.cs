using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpPromptArgument
{
    /// <summary>
    /// 매개변수 이름
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 매개변수 설명 (optional)
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 매개변수 타입 (optional, 기본값: string)
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 필수 여부 (optional, 기본값: false)
    /// </summary>
    [JsonProperty("required")]
    public bool? Required { get; set; }
}