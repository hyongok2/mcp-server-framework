using System;

namespace Micube.MCP.Core.Options;

public class LogOptions
{
    public string MinLevel { get; set; } = "Info";
    public LogFileOptions File { get; set; } = new();
}
