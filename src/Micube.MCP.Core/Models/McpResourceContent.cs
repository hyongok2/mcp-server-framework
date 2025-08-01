using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpResourceContent
{
    /// <summary>
    /// 리소스 URI
    /// </summary>
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// 텍스트 콘텐츠 (텍스트 리소스용)
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }

    /// <summary>
    /// MIME 타입
    /// </summary>
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }
}