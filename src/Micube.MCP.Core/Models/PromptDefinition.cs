using System;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Models;

public class PromptDefinition
{
    /// <summary>
    /// 프롬프트의 고유 식별자
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 프롬프트에 대한 설명
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 템플릿 파일 경로 (prompts 폴더 기준 상대경로)
    /// </summary>
    [JsonProperty("templateFile")]
    public string TemplateFile { get; set; } = string.Empty;

    /// <summary>
    /// 프롬프트 매개변수들
    /// </summary>
    [JsonProperty("arguments")]
    public List<McpPromptArgument>? Arguments { get; set; }

    /// <summary>
    /// 내부 모델을 MCP 표준 모델로 변환
    /// </summary>
    public McpPrompt ToMcpPrompt()
    {
        return new McpPrompt
        {
            Name = this.Name,
            Description = this.Description,
            Arguments = this.Arguments
        };
    }
}