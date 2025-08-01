using System;

namespace Micube.MCP.Core.Options;

public class LogFileOptions
{
    public string Directory { get; set; } = "logs";
    public int FlushIntervalSeconds { get; set; } = 2;
    public int MaxFileSizeMB { get; set; } = 50;
    public int RetentionDays { get; set; } = 30;
}
