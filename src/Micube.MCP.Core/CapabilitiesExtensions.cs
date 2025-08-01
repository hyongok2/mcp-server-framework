using System;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Utils;

namespace Micube.MCP.Core;

public static class CapabilitiesExtensions
{
    /// <summary>
    /// Tools 기능 지원 여부 확인
    /// </summary>
    public static bool SupportsTools(this ICapabilitiesService capabilities)
        => capabilities.IsFeatureSupported(CapabilitiesConstants.Features.Tools);

    /// <summary>
    /// Resources 기능 지원 여부 확인
    /// </summary>
    public static bool SupportsResources(this ICapabilitiesService capabilities)
        => capabilities.IsFeatureSupported(CapabilitiesConstants.Features.Resources);

    /// <summary>
    /// Prompts 기능 지원 여부 확인
    /// </summary>
    public static bool SupportsPrompts(this ICapabilitiesService capabilities)
        => capabilities.IsFeatureSupported(CapabilitiesConstants.Features.Prompts);

    /// <summary>
    /// Sampling 기능 지원 여부 확인
    /// </summary>
    public static bool SupportsSampling(this ICapabilitiesService capabilities)
        => capabilities.IsFeatureSupported(CapabilitiesConstants.Features.Sampling);

    /// <summary>
    /// Logging 기능 지원 여부 확인
    /// </summary>
    public static bool SupportsLogging(this ICapabilitiesService capabilities)
        => capabilities.IsFeatureSupported(CapabilitiesConstants.Features.Logging);

    // 알림 지원 여부 확인 메서드들
    public static bool SupportsToolsListChanged(this ICapabilitiesService capabilities)
        => capabilities.SupportsNotification(CapabilitiesConstants.Notifications.ToolsListChanged);

    public static bool SupportsResourcesListChanged(this ICapabilitiesService capabilities)
        => capabilities.SupportsNotification(CapabilitiesConstants.Notifications.ResourcesListChanged);

    public static bool SupportsResourcesUpdated(this ICapabilitiesService capabilities)
        => capabilities.SupportsNotification(CapabilitiesConstants.Notifications.ResourcesUpdated);

    public static bool SupportsPromptsListChanged(this ICapabilitiesService capabilities)
        => capabilities.SupportsNotification(CapabilitiesConstants.Notifications.PromptsListChanged);
}