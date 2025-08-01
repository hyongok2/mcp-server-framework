using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

public class ServerPromptsCapability
{
    /// <summary>
    /// 서버가 prompt 리스트 변경 알림을 보낼 수 있는지 여부
    /// </summary>
    [JsonProperty("listChanged")]
    public bool ListChanged { get; set; } = false;
}
