using System.Text.Json;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Loader;

public static class ToolGroupDescriptorParser
{
    public static ToolGroupMetadata? Parse(string manifestPath, IMcpLogger logger)
    {
        if (!File.Exists(manifestPath))
        {
            logger.LogError($"Manifest file not found: {manifestPath}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(manifestPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                logger.LogError($"Empty manifest file: {manifestPath}");
                return null;
            }

            var metadata = JsonSerializer.Deserialize<ToolGroupMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            return ValidateMetadata(metadata, manifestPath, logger);
        }
        catch (JsonException ex)
        {
            logger.LogError($"JSON parsing error in manifest {manifestPath}: {ex.Message}", ex);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to parse toolgroup manifest: {manifestPath}", ex);
            return null;
        }
    }

    private static ToolGroupMetadata? ValidateMetadata(ToolGroupMetadata? metadata, string manifestPath, IMcpLogger logger)
    {
        if (metadata == null)
        {
            logger.LogError($"Failed to deserialize manifest: {manifestPath}");
            return null;
        }

        // 1. GroupName 검증
        if (string.IsNullOrWhiteSpace(metadata.GroupName))
        {
            logger.LogError($"Missing or empty GroupName in manifest: {manifestPath}");
            return null;
        }

        // 2. Tools 배열 검증
        if (metadata.Tools == null)
        {
            logger.LogError($"Missing Tools array in manifest: {manifestPath}");
            return null;
        }

        // 3. 각 Tool 검증
        var toolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < metadata.Tools.Count; i++)
        {
            var tool = metadata.Tools[i];

            // Tool 이름 검증
            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                logger.LogError($"Tool at index {i} has missing or empty Name in group '{metadata.GroupName}'");
                return null;
            }

            // 중복 Tool 이름 검증
            if (!toolNames.Add(tool.Name))
            {
                logger.LogError($"Duplicate tool name '{tool.Name}' found in group '{metadata.GroupName}'");
                return null;
            }

            // 파라미터 검증
            if (!ValidateParameters(tool, metadata.GroupName, logger))
            {
                return null;
            }
        }

        logger.LogInfo($"✅ Validated metadata for ToolGroup '{metadata.GroupName}' with {metadata.Tools.Count} tools");
        return metadata;
    }

    private static bool ValidateParameters(ToolDescriptor tool, string groupName, IMcpLogger logger)
    {
        if (tool.Parameters == null)
        {
            tool.Parameters = new List<ParameterDescriptor>(); // 빈 리스트로 초기화
            return true;
        }

        var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in tool.Parameters)
        {
            // 파라미터 이름 검증
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                logger.LogError($"Parameter with empty name found in tool '{tool.Name}' of group '{groupName}'");
                return false;
            }

            // 중복 파라미터 이름 검증
            if (!paramNames.Add(param.Name))
            {
                logger.LogError($"Duplicate parameter name '{param.Name}' in tool '{tool.Name}' of group '{groupName}'");
                return false;
            }

            // 파라미터 타입 검증
            var validTypes = new[] { "string", "int", "number", "bool", "boolean", "object", "array" };
            if (!validTypes.Contains(param.Type.ToLowerInvariant()))
            {
                logger.LogError($"Invalid parameter type '{param.Type}' for parameter '{param.Name}' in tool '{tool.Name}' of group '{groupName}'");
                return false;
            }
        }

        return true;
    }
}
