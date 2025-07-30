using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpToolInfo
{
    /// <summary>
    /// 도구의 고유 식별자
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 도구에 대한 설명
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 도구의 입력 매개변수 스키마 (JSON Schema 형식)
    /// </summary>
    [JsonProperty("inputSchema")]
    public object? InputSchema { get; set; }
}
