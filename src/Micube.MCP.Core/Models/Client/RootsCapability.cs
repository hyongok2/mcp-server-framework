using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;
public class RootsCapability
{
    /// <summary>
    /// 클라이언트가 roots 리스트 변경 알림을 지원하는지 여부
    /// </summary>
    [JsonProperty("listChanged")]
    public bool ListChanged { get; set; } = false;
}