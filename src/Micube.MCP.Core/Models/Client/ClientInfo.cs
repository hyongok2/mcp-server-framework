using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

public class ClientInfo
{
    /// <summary>
    /// 클라이언트 이름
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 클라이언트 버전
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 클라이언트 설명
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
}
