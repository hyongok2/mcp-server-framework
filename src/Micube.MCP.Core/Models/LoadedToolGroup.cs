using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Models;

public class LoadedToolGroup
{
    public string GroupName { get; set; } = default!;
    public string ManifestPath { get; set; } = default!;
    public string Description { get; set; } = "";
    public IMcpToolGroup GroupInstance { get; set; } = default!;
    public ToolGroupMetadata? Metadata { get; set; } // 추후 파싱된 정보
}