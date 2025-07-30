using System;

namespace Micube.MCP.Server.Options;

public class FeatureOptions
{
    public bool EnableStdio { get; set; } = true;
    public bool EnableHttp { get; set; } = true;
}