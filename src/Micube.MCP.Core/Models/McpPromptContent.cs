using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpPromptContent
{
    /// <summary>
    /// 콘텐츠 타입 (text, image 등)
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// 텍스트 내용
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }

    /// <summary>
    /// 이미지 URL (type이 image인 경우)
    /// </summary>
    [JsonProperty("imageUrl")]
    public string? ImageUrl { get; set; }
}