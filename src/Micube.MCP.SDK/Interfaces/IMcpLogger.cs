using System;

namespace Micube.MCP.SDK.Interfaces;

public interface IMcpLogger
{
    void LogInfo(string message);
    void LogDebug(string message);
    void LogError(string message, Exception? ex = null);
    
    void LogInfo(string message, object? requestId);
    void LogDebug(string message, object? requestId);
    void LogError(string message, object? requestId, Exception? ex = null);
    
    Task ShutdownAsync();
}