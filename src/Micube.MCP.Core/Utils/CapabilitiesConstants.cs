using System;

namespace Micube.MCP.Core.Utils;

public static class CapabilitiesConstants
{
    /// <summary>
    /// MCP 기능 타입 상수
    /// </summary>
    public static class Features
    {
        public const string Tools = "tools";
        public const string Resources = "resources";
        public const string Prompts = "prompts";
        public const string Sampling = "sampling";
        public const string Logging = "logging";
    }

    /// <summary>
    /// MCP 알림 타입 상수
    /// </summary>
    public static class Notifications
    {
        public const string ToolsListChanged = "tools/listChanged";
        public const string ResourcesListChanged = "resources/listChanged";
        public const string ResourcesUpdated = "resources/updated";
        public const string PromptsListChanged = "prompts/listChanged";
        
        // 향후 확장 가능한 알림들
        public const string SamplingProgress = "sampling/progress";
        public const string LoggingLevelChanged = "logging/levelChanged";
    }

    /// <summary>
    /// 지원되는 모든 기능 목록 (검증용)
    /// </summary>
    public static readonly string[] SupportedFeatures = 
    {
        Features.Tools,
        Features.Resources,
        Features.Prompts,
        Features.Sampling,
        Features.Logging
    };

    /// <summary>
    /// 지원되는 모든 알림 목록 (검증용)
    /// </summary>
    public static readonly string[] SupportedNotifications = 
    {
        Notifications.ToolsListChanged,
        Notifications.ResourcesListChanged,
        Notifications.ResourcesUpdated,
        Notifications.PromptsListChanged,
        Notifications.SamplingProgress,
        Notifications.LoggingLevelChanged
    };
}
