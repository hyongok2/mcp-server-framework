using System;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Streamable.Interface;

namespace Micube.MCP.Core.Streamable.Models;

public class LoadedStreamableToolGroup
{
    public string GroupName { get; set; } = default!;
    public string ManifestPath { get; set; } = default!;
    public string Description { get; set; } = "";
    public IStreamableMcpToolGroup GroupInstance { get; set; } = default!;
    public ToolGroupMetadata? Metadata { get; set; } // 추후 파싱된 정보
}
