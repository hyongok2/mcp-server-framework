using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

/// <summary>
/// MCP 클라이언트가 지원하는 기능들을 나타내는 클래스
/// </summary>
public class ClientCapabilities
{
    /// <summary>
    /// 클라이언트가 Tools 기능을 지원하는지 여부
    /// true: 클라이언트가 tools/list, tools/call을 사용할 수 있음
    /// </summary>
    [JsonProperty("tools")]
    public bool? Tools { get; set; }

    /// <summary>
    /// 클라이언트가 Resources 기능을 지원하는지 여부
    /// true: 클라이언트가 resources/list, resources/read를 사용할 수 있음
    /// </summary>
    [JsonProperty("resources")]
    public bool? Resources { get; set; }

    /// <summary>
    /// 클라이언트가 Prompts 기능을 지원하는지 여부
    /// true: 클라이언트가 prompts/list, prompts/get을 사용할 수 있음
    /// </summary>
    [JsonProperty("prompts")]
    public bool? Prompts { get; set; }

    /// <summary>
    /// 클라이언트가 Sampling 기능을 지원하는지 여부
    /// true: 서버가 클라이언트에게 sampling 요청을 보낼 수 있음
    /// </summary>
    [JsonProperty("sampling")]
    public bool? Sampling { get; set; }

    /// <summary>
    /// 클라이언트가 Logging 기능을 지원하는지 여부
    /// true: 서버가 클라이언트에게 로그 메시지를 보낼 수 있음
    /// </summary>
    [JsonProperty("logging")]
    public bool? Logging { get; set; }

    /// <summary>
    /// 클라이언트가 Roots 기능을 지원하는지 여부 (객체 형태)
    /// </summary>
    [JsonProperty("roots")]
    public RootsCapability? Roots { get; set; }
}
