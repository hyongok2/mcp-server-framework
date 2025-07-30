using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class McpErrorResponse
{
    /// <summary>
    /// JSON-RPC 버전 (항상 "2.0")
    /// </summary>
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// 메시지 식별자. 요청-응답 매칭에 사용됩니다.
    /// </summary>
    [JsonProperty("id")]
    public object? Id { get; set; }

    /// <summary>
    /// 에러 정보
    /// </summary>
    [JsonProperty("error")]
    public McpError Error { get; set; } = new McpError();
}