using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models.Client;

public class ServerCapabilities
{
    /// <summary>
    /// 서버가 Tools 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("tools")]
    public ServerToolsCapability? Tools { get; set; }

    /// <summary>
    /// 서버가 Resources 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("resources")]
    public ServerResourcesCapability? Resources { get; set; }

    /// <summary>
    /// 서버가 Prompts 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("prompts")]
    public ServerPromptsCapability? Prompts { get; set; }

    /// <summary>
    /// 서버가 Logging 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("logging")]
    public ServerLoggingCapability? Logging { get; set; }
}
