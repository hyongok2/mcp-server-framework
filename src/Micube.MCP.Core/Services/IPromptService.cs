using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Services;

public interface IPromptService
{
    /// <summary>
    /// 사용 가능한 모든 프롬프트 목록을 반환합니다
    /// </summary>
    Task<List<McpPrompt>> GetPromptsAsync();

    /// <summary>
    /// 특정 프롬프트를 실행하여 렌더링된 메시지를 생성합니다
    /// </summary>
    Task<McpPromptResponse?> GetPromptAsync(string name, Dictionary<string, object>? arguments = null);
}