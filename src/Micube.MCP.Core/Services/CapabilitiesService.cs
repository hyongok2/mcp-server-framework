using System;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Services;

public class CapabilitiesService : ICapabilitiesService
{
    private readonly IMcpLogger _logger;
    private readonly object _lock = new object();

    private ClientCapabilities? _clientCapabilities;
    private ClientInfo? _clientInfo;
    private readonly ServerCapabilities _serverCapabilities;

    public CapabilitiesService(IMcpLogger logger)
    {
        _logger = logger;
        _serverCapabilities = CreateServerCapabilities();
    }

    public CapabilitiesValidationResult ValidateAndStore(ClientInitializeParams clientParams)
    {
        lock (_lock)
        {
            try
            {
                // 1. 프로토콜 버전 검증
                if (!IsProtocolVersionSupported(clientParams.ProtocolVersion))
                {
                    var error = $"Unsupported protocol version: {clientParams.ProtocolVersion}. Expected: {JsonRpcConstants.ProtocolVersion}";
                    _logger.LogError($"[Capabilities] {error}");
                    return CapabilitiesValidationResult.Error(error);
                }

                // 2. 클라이언트 정보 검증
                if (string.IsNullOrEmpty(clientParams.ClientInfo.Name))
                {
                    var error = "Client name is required";
                    _logger.LogError($"[Capabilities] {error}");
                    return CapabilitiesValidationResult.Error(error);
                }

                // 3. Capabilities 저장
                _clientCapabilities = clientParams.Capabilities ?? new ClientCapabilities();
                _clientInfo = clientParams.ClientInfo;

                _logger.LogInfo($"[Capabilities] Client registered: {_clientInfo.Name} v{_clientInfo.Version}");
                _logger.LogDebug($"[Capabilities] Client supports - Tools: {_clientCapabilities.Tools != null}, Resources: {_clientCapabilities.Resources != null}, Prompts: {_clientCapabilities.Prompts != null}");

                return CapabilitiesValidationResult.Success();
            }
            catch (Exception ex)
            {
                var error = $"Failed to validate client capabilities: {ex.Message}";
                _logger.LogError($"[Capabilities] {error}", ex);
                return CapabilitiesValidationResult.Error(error);
            }
        }
    }

    public ClientCapabilities? GetClientCapabilities()
    {
        lock (_lock)
        {
            return _clientCapabilities;
        }
    }

    public ClientInfo? GetClientInfo()
    {
        lock (_lock)
        {
            return _clientInfo;
        }
    }

    public ServerCapabilities GetServerCapabilities()
    {
        return _serverCapabilities;
    }

    public bool IsFeatureSupported(string featureName)
    {
        lock (_lock)
        {
            if (_clientCapabilities == null) return false;

            return featureName.ToLowerInvariant() switch
            {
                CapabilitiesConstants.Features.Tools => _clientCapabilities.Tools != null,
                CapabilitiesConstants.Features.Resources => _clientCapabilities.Resources != null,
                CapabilitiesConstants.Features.Prompts => _clientCapabilities.Prompts != null,
                CapabilitiesConstants.Features.Sampling => _clientCapabilities.Sampling != null,
                CapabilitiesConstants.Features.Logging => _clientCapabilities.Logging != null,
                _ => false
            };
        }
    }

    public bool SupportsNotification(string notificationType)
    {
        lock (_lock)
        {
            if (_clientCapabilities == null) return false;

            return notificationType.ToLowerInvariant() switch
            {
                CapabilitiesConstants.Notifications.ToolsListChanged => _clientCapabilities.Tools?.ListChanged ?? false,
                CapabilitiesConstants.Notifications.ResourcesListChanged => _clientCapabilities.Resources?.ListChanged ?? false,
                CapabilitiesConstants.Notifications.ResourcesUpdated => _clientCapabilities.Resources?.Subscribe ?? false,
                CapabilitiesConstants.Notifications.PromptsListChanged => _clientCapabilities.Prompts?.ListChanged ?? false,
                CapabilitiesConstants.Notifications.SamplingProgress => _clientCapabilities.Sampling != null, // 향후 구현
                CapabilitiesConstants.Notifications.LoggingLevelChanged => _clientCapabilities.Logging != null, // 향후 구현
                _ => false
            };
        }
    }

        /// <summary>
    /// 기능 이름이 유효한지 검증합니다
    /// </summary>
    public bool IsValidFeatureName(string featureName)
    {
        return CapabilitiesConstants.SupportedFeatures.Contains(featureName.ToLowerInvariant());
    }

    /// <summary>
    /// 알림 타입이 유효한지 검증합니다
    /// </summary>
    public bool IsValidNotificationType(string notificationType)
    {
        return CapabilitiesConstants.SupportedNotifications.Contains(notificationType.ToLowerInvariant());
    }

    private static bool IsProtocolVersionSupported(string clientVersion)
    {
        // 현재는 정확한 버전 매칭만 지원
        // 향후 버전 호환성 로직 추가 가능
        return clientVersion == JsonRpcConstants.ProtocolVersion;
    }

    private static ServerCapabilities CreateServerCapabilities()
    {
        return new ServerCapabilities
        {
            Tools = new ServerToolsCapability
            {
                ListChanged = false // 현재 구현되지 않음
            },
            Resources = new ServerResourcesCapability
            {
                Subscribe = false,   // 향후 구현 예정
                ListChanged = false  // 향후 구현 예정
            },
            Prompts = new ServerPromptsCapability
            {
                ListChanged = false  // 향후 구현 예정
            },
            Logging = new ServerLoggingCapability()
        };
    }
}

