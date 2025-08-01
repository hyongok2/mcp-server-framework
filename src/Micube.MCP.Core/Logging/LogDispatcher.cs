using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Logging;

/// <summary>
/// 다양한 방식으로 로깅할 수 있도록 하는 로깅 디스패처입니다.
/// 이 클래스는 IMcpLogger 인터페이스를 구현하여 로깅 기능을 제공합니다
/// </summary>
public class LogDispatcher : IMcpLogger
{
    private readonly IEnumerable<ILogWriter> _writers;
    private readonly LogLevel _minLevel;

    public LogDispatcher(IEnumerable<ILogWriter> writers, LogLevel minLevel)
    {
        _writers = writers;
        _minLevel = minLevel;
    }

    public bool IsDebugLevel()
    {
        return _minLevel <= LogLevel.Debug;
    }

    public void LogDebug(string message)
    {
        Write(LogLevel.Debug, message);
    }

    public void LogInfo(string message)
    {
        Write(LogLevel.Info, message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        Write(LogLevel.Error, message, ex);
    }

    public void LogInfo(string message, object? requestId)
    {
        Write(LogLevel.Info, FormatWithRequestId(message, requestId));
    }

    public void LogDebug(string message, object? requestId)
    {
        Write(LogLevel.Debug, FormatWithRequestId(message, requestId));
    }

    public void LogError(string message, object? requestId, Exception? ex = null)
    {
        Write(LogLevel.Error, FormatWithRequestId(message, requestId), ex);
    }

    private static string FormatWithRequestId(string message, object? requestId)
    {
        return requestId != null ? $"[ID:{requestId}] {message}" : message;
    }

    private void Write(LogLevel level, string message, Exception? ex = null)
    {
        if (level < _minLevel) return;

        var logItem = new LogItem
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Exception = ex
        };

        foreach (var writer in _writers)
        {
            writer.Write(logItem);
        }
    }

    public async Task ShutdownAsync()
    {
        foreach (var writer in _writers)
        {
            await writer.ShutdownAsync();
        }
    }
}
