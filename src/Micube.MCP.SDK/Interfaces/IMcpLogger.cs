using System;

namespace Micube.MCP.SDK.Interfaces;

public interface IMcpLogger
{
    void LogInfo(string message);
    void LogDebug(string message);
    void LogError(string message, Exception? ex = null);
}