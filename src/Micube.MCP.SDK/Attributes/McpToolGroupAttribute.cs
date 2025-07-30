using System;

namespace Micube.MCP.SDK.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class McpToolGroupAttribute : Attribute
{
    public string GroupName { get; }

    // toolgroup의 menifest 파일(json)의 상대 경로 및 파일명 (기본값은 동일 디렉터리 내)
    public string ManifestPath { get; }
    public string? Description { get; set; }

    public McpToolGroupAttribute(string groupName, string? manifestPath, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("Group name cannot be null or empty.", nameof(groupName));

        if (string.IsNullOrWhiteSpace(manifestPath))
            throw new ArgumentException("Manifest path cannot be null or empty.", nameof(manifestPath));

        if (!manifestPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Manifest path must end with .json", nameof(manifestPath));
            
        GroupName = groupName;
        ManifestPath = manifestPath;
        Description = description;
    }
}