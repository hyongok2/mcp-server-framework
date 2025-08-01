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
    /// </summary>
    [JsonProperty("tools")]
    public ToolsCapability? Tools { get; set; }

    /// <summary>
    /// 클라이언트가 Resources 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("resources")]
    public ResourcesCapability? Resources { get; set; }

    /// <summary>
    /// 클라이언트가 Prompts 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("prompts")]
    public PromptsCapability? Prompts { get; set; }

    /// <summary>
    /// 클라이언트가 Sampling 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("sampling")]
    public SamplingCapability? Sampling { get; set; }

    /// <summary>
    /// 클라이언트가 Logging 기능을 지원하는지 여부
    /// </summary>
    [JsonProperty("logging")]
    public LoggingCapability? Logging { get; set; }
}