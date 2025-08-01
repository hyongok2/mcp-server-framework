using System;
using Newtonsoft.Json;

namespace Micube.MCP.SDK.Models;

public class ToolContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "text"; // text, image, code, structured 등

    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("data")]
    public object? Data { get; set; }  // 구조화된 데이터용

    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }

    // 새로 추가: 구조화된 출력용
    [JsonProperty("schema")]
    public object? Schema { get; set; }  // JSON Schema

    public ToolContent() { }

    public ToolContent(string type, string? text)
    {
        Type = type;
        Text = text;
    }

    // 새로 추가: 구조화된 데이터 생성자
    public ToolContent(object data, object? schema = null)
    {
        Type = "structured";
        Data = data;
        Schema = schema;
    }
}