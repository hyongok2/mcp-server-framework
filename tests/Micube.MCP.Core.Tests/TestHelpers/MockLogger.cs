using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Tests.TestHelpers;

public class MockLogger : IMcpLogger
{
    public List<string> InfoMessages { get; } = new();
    public List<string> DebugMessages { get; } = new();
    public List<(string Message, Exception? Exception)> ErrorMessages { get; } = new();

    public void LogInfo(string message)
    {
        InfoMessages.Add(message);
    }

    public void LogDebug(string message)
    {
        DebugMessages.Add(message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        ErrorMessages.Add((message, ex));
    }

    public void LogInfo(string message, object? requestId)
    {
        InfoMessages.Add($"[{requestId}] {message}");
    }

    public void LogDebug(string message, object? requestId)
    {
        DebugMessages.Add($"[{requestId}] {message}");
    }

    public void LogError(string message, object? requestId, Exception? ex = null)
    {
        ErrorMessages.Add(($"[{requestId}] {message}", ex));
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }

    public void Clear()
    {
        InfoMessages.Clear();
        DebugMessages.Clear();
        ErrorMessages.Clear();
    }
}