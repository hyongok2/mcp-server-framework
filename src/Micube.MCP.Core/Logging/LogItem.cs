using System;

namespace Micube.MCP.Core.Logging;

public class LogItem
{
    /// <summary>
    /// 로그 항목이 생성된 시간입니다.
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// 로그 항목의 심각도 수준입니다.
    /// </summary>
    public LogLevel Level { get; init; }
    
    /// <summary>
    /// 로그 메시지 내용입니다.
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 로그 항목과 관련된 예외 객체입니다. 오류 로그에서 주로 사용됩니다.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// 로그를 생성한 스레드의 ID입니다. 기본값은 현재 관리되는 스레드 ID입니다.
    /// </summary>
    public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
}
