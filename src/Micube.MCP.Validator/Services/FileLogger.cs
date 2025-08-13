using System.Collections.Concurrent;
using System.Text;

namespace Micube.MCP.Validator.Services;

public class FileLogger : IDisposable
{
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<string> _logQueue;
    private readonly Timer _flushTimer;
    private readonly object _lockObject = new();
    private bool _disposed;

    public FileLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"mcp-validator_{timestamp}.log");
        _logQueue = new ConcurrentQueue<string>();

        // 5초마다 로그를 파일에 플러시
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        LogInfo("FileLogger", "Logger initialized", $"Log file: {_logFilePath}");
    }

    public void LogInfo(string category, string message, string? details = null)
    {
        WriteLog("INFO", category, message, details);
    }

    public void LogWarning(string category, string message, string? details = null)
    {
        WriteLog("WARN", category, message, details);
    }

    public void LogError(string category, string message, string? details = null, Exception? ex = null)
    {
        var errorDetails = details;
        if (ex != null)
        {
            errorDetails = $"{details}\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        }
        WriteLog("ERROR", category, message, errorDetails);
    }

    public void LogCritical(string category, string message, string? details = null, Exception? ex = null)
    {
        var errorDetails = details;
        if (ex != null)
        {
            errorDetails = $"{details}\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        }
        WriteLog("CRITICAL", category, message, errorDetails);
    }

    public void LogValidationStart(string validatorName, string? filePath = null)
    {
        var details = filePath != null ? $"File: {filePath}" : null;
        WriteLog("START", validatorName, "Validation started", details);
    }

    public void LogValidationEnd(string validatorName, string? filePath = null, TimeSpan? duration = null)
    {
        var details = filePath != null ? $"File: {filePath}" : null;
        if (duration.HasValue)
        {
            details += $" | Duration: {duration.Value.TotalMilliseconds:F0}ms";
        }
        WriteLog("END", validatorName, "Validation completed", details);
    }

    private void WriteLog(string level, string category, string message, string? details = null)
    {
        if (_disposed) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = Environment.CurrentManagedThreadId;
        
        var logEntry = new StringBuilder();
        logEntry.Append($"[{timestamp}] [{level,-8}] [{threadId,3}] [{category,-15}] {message}");
        
        if (!string.IsNullOrEmpty(details))
        {
            logEntry.Append($"\n    Details: {details.Replace("\n", "\n             ")}");
        }

        _logQueue.Enqueue(logEntry.ToString());
    }

    private void FlushLogs(object? state)
    {
        if (_disposed) return;

        var logsToWrite = new List<string>();
        
        while (_logQueue.TryDequeue(out var log))
        {
            logsToWrite.Add(log);
        }

        if (logsToWrite.Count == 0) return;

        lock (_lockObject)
        {
            try
            {
                File.AppendAllLines(_logFilePath, logsToWrite, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 로그 파일 쓰기 실패 시 콘솔에 출력
                Console.WriteLine($"[FileLogger Error] Failed to write to log file: {ex.Message}");
            }
        }
    }

    public void Flush()
    {
        FlushLogs(null);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        
        // 마지막 로그들 플러시
        FlushLogs(null);
        
        // 타이머 정리
        _flushTimer?.Dispose();
        
        LogInfo("FileLogger", "Logger disposed");
        FlushLogs(null); // 마지막 메시지도 플러시
    }
}