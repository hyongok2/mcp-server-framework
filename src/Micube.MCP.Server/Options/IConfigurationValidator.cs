using System;
using Micube.MCP.Core.Options;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Server.Options;

/// <summary>
/// 설정 검증 인터페이스
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// 모든 설정을 검증하고 필요한 디렉토리를 생성합니다
    /// </summary>
    void ValidateAndSetup(
        ToolGroupOptions toolGroupOptions,
        ResourceOptions resourceOptions,
        PromptOptions promptOptions,
        LogOptions logOptions,
        FeatureOptions featureOptions,
        IMcpLogger logger);
}