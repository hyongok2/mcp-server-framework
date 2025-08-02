using Microsoft.Extensions.Options;
using Micube.MCP.Core.Options;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Server.Options;

/// <summary>
/// 설정 검증 유틸리티 클래스
/// SystemContextHostedService에서 사용됩니다.
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// 모든 설정을 검증하고 필요한 디렉토리를 생성합니다
    /// </summary>
    public static void ValidateAndSetup(
        ToolGroupOptions toolGroupOptions,
        ResourceOptions resourceOptions,
        PromptOptions promptOptions,
        LogOptions logOptions,
        FeatureOptions featureOptions,
        IMcpLogger logger)
    {
        logger.LogInfo("Starting configuration validation...");
        
        var validationErrors = new List<string>();

        // 1. 기본 기능 설정 검증
        ValidateFeatureOptions(featureOptions, validationErrors, logger);

        // 2. 디렉토리 경로 검증 및 생성
        ValidateAndCreateDirectories(toolGroupOptions, resourceOptions, promptOptions, logOptions, validationErrors, logger);

        // 3. 도구 화이트리스트 검증
        ValidateToolWhitelist(toolGroupOptions, validationErrors, logger);

        // 4. 로그 설정 검증
        ValidateLogOptions(logOptions, validationErrors, logger);

        if (validationErrors.Any())
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", validationErrors);
            logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        logger.LogInfo("Configuration validation completed successfully.");
    }

    private static void ValidateFeatureOptions(FeatureOptions featureOptions, List<string> errors, IMcpLogger logger)
    {
        if (!featureOptions.EnableStdio && !featureOptions.EnableHttp)
        {
            errors.Add("At least one transport method (STDIO or HTTP) must be enabled.");
        }
        else
        {
            logger.LogInfo($"Transport modes - STDIO: {featureOptions.EnableStdio}, HTTP: {featureOptions.EnableHttp}");
        }
    }

    private static void ValidateAndCreateDirectories(
        ToolGroupOptions toolGroupOptions,
        ResourceOptions resourceOptions, 
        PromptOptions promptOptions,
        LogOptions logOptions,
        List<string> errors, 
        IMcpLogger logger)
    {
        var directories = new Dictionary<string, string>
        {
            ["Tools"] = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, toolGroupOptions.Directory)),
            ["Resources"] = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resourceOptions.Directory)),
            ["Prompts"] = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, promptOptions.Directory)),
            ["Logs"] = Path.GetFullPath(logOptions.File.Directory)
        };

        foreach (var (name, path) in directories)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    logger.LogInfo($"Created {name} directory: {path}");
                }
                else
                {
                    logger.LogInfo($"Verified {name} directory: {path}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create {name} directory '{path}': {ex.Message}");
            }
        }
    }

    private static void ValidateToolWhitelist(ToolGroupOptions toolGroupOptions, List<string> errors, IMcpLogger logger)
    {
        if (toolGroupOptions.Whitelist?.Any() == true)
        {
            var toolsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, toolGroupOptions.Directory));
            var missingDlls = toolGroupOptions.Whitelist
                .Where(dll => !File.Exists(Path.Combine(toolsPath, dll)))
                .ToList();

            if (missingDlls.Any())
            {
                logger.LogError($"Warning: Some whitelisted tool DLLs not found: {string.Join(", ", missingDlls)}");
                // Warning이지 에러는 아님 - 나중에 추가될 수 있음
            }
        }
    }

    private static void ValidateLogOptions(LogOptions logOptions, List<string> errors, IMcpLogger logger)
    {
        if (logOptions.File.MaxFileSizeMB <= 0)
        {
            errors.Add("Log file maximum size must be greater than 0.");
        }

        if (logOptions.File.RetentionDays <= 0)
        {
            errors.Add("Log file retention days must be greater than 0.");
        }

        if (logOptions.File.FlushIntervalSeconds <= 0)
        {
            errors.Add("Log flush interval must be greater than 0.");
        }

        if (errors.Count == 0)
        {
            logger.LogInfo($"Log configuration - MaxSize: {logOptions.File.MaxFileSizeMB}MB, Retention: {logOptions.File.RetentionDays} days");
        }
    }
}