using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Services;

public interface IResourceService
{
    /// <summary>
    /// 사용 가능한 모든 리소스 목록을 반환합니다
    /// </summary>
    Task<List<McpResource>> GetResourcesAsync();

    /// <summary>
    /// 특정 URI의 리소스 내용을 읽어옵니다
    /// </summary>
    Task<McpResourceContent?> ReadResourceAsync(string uri);

    /// <summary>
    /// 리소스 목록을 새로고침합니다 (파일 시스템 변경 감지)
    /// </summary>
    Task RefreshResourcesAsync();
}