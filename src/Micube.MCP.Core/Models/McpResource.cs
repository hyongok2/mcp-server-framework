using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpResource
{
    /// <summary>
    /// 리소스의 고유 식별자 URI
    /// </summary>
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// 사람이 읽을 수 있는 리소스 이름
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 리소스에 대한 설명
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// MIME 타입
    /// </summary>
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    /// 파일 크기 (바이트)
    /// </summary>
    [JsonProperty("size")]
    public long? Size { get; set; }
}