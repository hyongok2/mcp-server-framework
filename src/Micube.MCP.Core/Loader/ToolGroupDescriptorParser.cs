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
            var metadata = JsonSerializer.Deserialize<ToolGroupMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata == null || string.IsNullOrWhiteSpace(metadata.GroupName))
            {
                logger.LogError($"Invalid or empty metadata in {manifestPath}");
                return null;
            }

            logger.LogInfo($"Parsed metadata for ToolGroup '{metadata.GroupName}' with {metadata.Tools.Count} tools.");
            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to parse toolgroup manifest: {manifestPath}", ex);
            return null;
        }
    }
}
