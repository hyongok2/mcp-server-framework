using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

public class ClientInitializeParams
{
    /// <summary>
    /// 클라이언트가 지원하는 MCP 프로토콜 버전
    /// </summary>
    [JsonProperty("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;

    /// <summary>
    /// 클라이언트 정보
    /// </summary>
    [JsonProperty("clientInfo")]
    public ClientInfo ClientInfo { get; set; } = new();

    /// <summary>
    /// 클라이언트가 지원하는 기능들
    /// </summary>
    [JsonProperty("capabilities")]
    public ClientCapabilities Capabilities { get; set; } = new();
}
