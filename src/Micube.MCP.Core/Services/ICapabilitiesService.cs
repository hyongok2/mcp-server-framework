using System;
using Micube.MCP.Core.Models.Client;

namespace Micube.MCP.Core.Services;

public interface ICapabilitiesService
{
    /// <summary>
    /// 클라이언트 capabilities를 검증하고 저장합니다
    /// </summary>
    /// <param name="clientParams">클라이언트 초기화 파라미터</param>
    /// <returns>검증 결과</returns>
    CapabilitiesValidationResult ValidateAndStore(ClientInitializeParams clientParams);

    /// <summary>
    /// 현재 클라이언트 capabilities를 반환합니다
    /// </summary>
    ClientCapabilities? GetClientCapabilities();

    /// <summary>
    /// 현재 클라이언트 정보를 반환합니다
    /// </summary>
    ClientInfo? GetClientInfo();

    /// <summary>
    /// 서버 capabilities를 반환합니다
    /// </summary>
    ServerCapabilities GetServerCapabilities();

    /// <summary>
    /// 특정 기능이 클라이언트와 서버 모두에서 지원되는지 확인합니다
    /// </summary>
    bool IsFeatureSupported(string featureName);

    /// <summary>
    /// 기능 이름이 유효한지 검증합니다
    /// </summary>
    bool IsValidFeatureName(string featureName);

    /// <summary>
    /// 알림 타입이 유효한지 검증합니다
    /// </summary>
    bool IsValidNotificationType(string notificationType);

}

