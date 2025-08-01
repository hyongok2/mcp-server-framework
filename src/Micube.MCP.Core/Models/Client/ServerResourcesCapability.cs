using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

public class ServerResourcesCapability
{
    /// <summary>
    /// 서버가 resource 구독을 지원하는지 여부
    /// </summary>
    [JsonProperty("subscribe")]
    public bool Subscribe { get; set; } = false;

    /// <summary>
    /// 서버가 resource 리스트 변경 알림을 보낼 수 있는지 여부
    /// </summary>
    [JsonProperty("listChanged")]
    public bool ListChanged { get; set; } = false;
}