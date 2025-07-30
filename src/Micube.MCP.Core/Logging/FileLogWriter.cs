using System;
using System.Collections.Concurrent;
using System.Text;

namespace Micube.MCP.Core.Logging;

public class FileLogWriter : ILogWriter, IDisposable
{
    #region Fields
    /// <summary>
    /// 로그 파일이 저장될 기본 디렉토리 경로입니다.
    /// </summary>
    private readonly string _baseDirectory;

    /// <summary>
    /// 버퍼에 있는 로그를 파일에 기록하는 주기(초)입니다.
    /// </summary>
    private readonly int _flushIntervalSeconds;
    /// <summary>
    /// 로그 파일의 최대 크기(MB)입니다. 이 크기를 초과하면 파일 로테이션이 발생합니다.
    /// </summary>
    private readonly int _maxFileSizeMB;
    /// <summary>
    /// 로그 항목을 저장하는 비동기 큐입니다.
    /// </summary>
    private readonly BlockingCollection<LogItem> _queue = new();
    /// <summary>
    /// 파일 경로별 로그 메시지 버퍼입니다.
    /// 워커 스레드에서만 접근되므로 동기화가 필요하지 않습니다.
    /// </summary>
    private readonly Dictionary<string, List<string>> _buffers = new();
    /// <summary>
    /// 로그 처리를 담당하는 백그라운드 스레드입니다.
    /// </summary>
    private readonly Thread _workerThread;
    /// <summary>
    /// 객체가 이미 삭제되었는지 여부를 나타내는 플래그입니다.
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 로그 레벨 문자열을 캐싱하는 정적 딕셔너리입니다.
    /// 반복적인 문자열 생성을 방지하여 성능을 향상시킵니다.
    /// </summary>
    private static readonly Dictionary<LogLevel, string> _cachedLevels = new Dictionary<LogLevel, string>();
    /// <summary>
    /// 카테고리별 파일 경로를 캐싱하는 딕셔너리입니다.
    /// </summary>
    private string _pathCache = string.Empty;

    private DateTime _current = DateTime.Now;

    /// <summary>
    /// 로그 파일의 보존 기간(일)입니다.
    /// 이 값은 파일 로테이션과 관련된 설정으로, 오래된 로그 파일을 자동으로 삭제하는 데 사용됩니다.
    /// 기본값은 30일입니다.
    /// </summary>
    private readonly int _retentionDays;

    /// <summary>
    /// 로그 파일 정리를 위한 백그라운드 스레드입니다.
    /// 이 스레드는 주기적으로 오래된 로그 파일을 삭제합니다.
    /// </summary>
    private readonly Thread _cleanupThread;

    /// <summary>
    /// Cleanup 작업을 위한 CancellationTokenSource입니다.
    /// 이 객체는 로그 파일 정리 작업을 안전하게 중단할 수 있도록 합니다
    /// </summary>
    private readonly CancellationTokenSource _ctsForCleanUpThread = new();

    #endregion Fields

    #region Constructors
    /// <summary>
    /// FileLogWriter의 새 인스턴스를 초기홉합니다.
    /// </summary>
    /// <param name="baseDirectory">로그 파일이 저장될 기본 디렉토리 경로</param>
    /// <param name="flushIntervalSeconds">버퍼 플러시 주기(초)</param>
    /// <param name="maxFileSizeMB">로그 파일의 최대 크기(MB)</param>
    /// <param name="retentionDays">로그 파일의 보존 기간(일)</param>
    /// <param name="emergencyLogger">비상 로거 (선택 사항)</param>
    public FileLogWriter(string baseDirectory,
                        int flushIntervalSeconds = 2,
                        int maxFileSizeMB = 50,
                        int retentionDays = 30)
    {
        _baseDirectory = baseDirectory;
        _flushIntervalSeconds = flushIntervalSeconds;
        _maxFileSizeMB = maxFileSizeMB;
        _retentionDays = retentionDays;

        // 로그 레벨 문자열을 미리 캐싱하여 성능 향상
        foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
        {
            _cachedLevels[level] = level.ToString().ToUpper();
        }

        Directory.CreateDirectory(_baseDirectory);

        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "FileLogWriterThread"
        };
        _workerThread.Start();

        _cleanupThread = new Thread(() => CleanupWorkerAsync(_ctsForCleanUpThread.Token).GetAwaiter().GetResult())
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
            Name = "FileLogWriterCleanupThread"
        };
        _cleanupThread.Start();
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>
    /// 로그 항목을 큐에 추가합니다.
    /// </summary>
    /// <param name="item">추가할 로그 항목</param>
    public void Write(LogItem item)
    {
        _queue.Add(item);
    }
    /// <summary>
    /// FileLogWriter에서 사용하는 리소스를 해제합니다.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }
        catch { }
    }

    /// <summary>
    /// FileLogWriter를 안전하게 종료합니다.
    /// 큐에 남아있는 로그를 모두 처리하고 리소스를 정리합니다.
    /// </summary>
    /// <returns>종료 작업 완료를 나타내는 Task</returns>
    public async Task ShutdownAsync()
    {
        if (_disposed) return;

        try
        {
            _ctsForCleanUpThread.Cancel();
            _cleanupThread.Join();        // 워커 종료 대기
            _ctsForCleanUpThread.Dispose();
            _queue.CompleteAdding();      // 더 이상 로그 받지 않음
            _workerThread.Join();         // 백그라운드 쓰레드 종료 대기
            _disposed = true;
        }
        catch { }

        await Task.CompletedTask; // 비동기 시그니처 호환
    }
    #endregion Public Methods

    #region Private Methods

    #region Log Write Worker
    /// <summary>
    /// 큐에서 로그 항목을 가져와 버퍼에 추가하고 주기적으로 파일에 기록하는 백그라운드 작업을 처리합니다.
    /// </summary>
    private void WorkerLoop()
    {
        DateTime lastFlush = DateTime.Now;

        while (!_queue.IsCompleted)
        {
            try
            {
                DequeueMessage();

                if ((DateTime.Now - lastFlush).TotalSeconds >= _flushIntervalSeconds)
                {
                    FlushAll();
                    lastFlush = DateTime.Now;
                }

                Thread.Sleep(200);
            }
            catch { }
        }

        FlushAll(); // 종료 시 flush
    }

    /// <summary>
    /// 큐에서 로그 항목을 가져와 적절한 버퍼에 추가합니다.
    /// </summary>
    private void DequeueMessage()
    {
        while (_queue.TryTake(out var item))
        {
            string path = GetLogPath(item);
            string message = CreateMessage(item);

            if (!_buffers.ContainsKey(path))
                _buffers[path] = new List<string>();

            _buffers[path].Add(message);
        }
    }

    /// <summary>
    /// 모든 버퍼의 내용을 해당 파일에 기록합니다.
    /// </summary>
    private void FlushAll()
    {
        foreach (var entry in _buffers)
        {
            string path = entry.Key;
            var messages = entry.Value;

            if (messages.Count == 0) continue;

            string actualPath = GetRotatedLogFilePath(path);

            try
            {
                string? dir = Path.GetDirectoryName(actualPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllLines(actualPath, messages, Encoding.UTF8);
            }
            catch { }
        }
        _buffers.Clear();// flush 후 buffer 초기화
    }

    /// <summary>
    /// 로그 항목을 포맷팅하여 파일에 기록할 문자열을 생성합니다.
    /// </summary>
    /// <param name="item">포맷팅할 로그 항목</param>
    /// <returns>포맷팅된 로그 메시지 문자열</returns>
    private string CreateMessage(LogItem item)
    {
        var sb = new StringBuilder(256); // 적절한 초기 용량 지정

        sb.Append('[');
        sb.Append(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        sb.Append("] [");

        // 캐시된 로그 레벨 사용
        sb.Append(_cachedLevels[item.Level]);
        sb.Append("] [");
        sb.Append(item.ThreadId);
        sb.Append("] ");
        sb.Append(item.Message);

        if (item.Exception != null)
        {
            sb.Append(" | Exception: ");
            sb.Append(item.Exception);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 로그 항목의 카테고리와 날짜를 기반으로 로그 파일 경로를 결정합니다.
    /// 경로는 캐시되어 반복적인 경로 계산을 방지합니다.
    /// </summary>
    /// <param name="item">로그 항목</param>
    /// <returns>로그 파일 경로</returns>
    private string GetLogPath(LogItem item)
    {
        DateTime now = DateTime.Now;

        if (_pathCache != string.Empty && _current.Date == now.Date)
        {
            return _pathCache;
        }

        _current = now;
        _pathCache = Path.Combine(_baseDirectory, $"{DateTime.Now.ToString("yyyy-MM-dd")}.log");

        return _pathCache;
    }

    /// <summary>
    /// 파일 크기 기반 로테이션을 적용한 로그 파일 경로를 반환합니다.
    /// 파일 크기가 최대 크기를 초과하면 새 파일 경로를 생성합니다.
    /// </summary>
    /// <param name="originalPath">원본 로그 파일 경로</param>
    /// <returns>로테이션이 적용된 로그 파일 경로</returns>
    private string GetRotatedLogFilePath(string originalPath)
    {
        if (!File.Exists(originalPath))
            return originalPath;

        var info = new FileInfo(originalPath);
        if (info.Length < _maxFileSizeMB * 1024 * 1024)
            return originalPath;

        int index = 1;
        string dir = Path.GetDirectoryName(originalPath)!;
        string name = Path.GetFileNameWithoutExtension(originalPath);
        string ext = Path.GetExtension(originalPath);

        string rotatedPath;
        do
        {
            rotatedPath = Path.Combine(dir, $"{name}_{index}{ext}");
            index++;
        } while (File.Exists(rotatedPath) && new FileInfo(rotatedPath).Length >= _maxFileSizeMB * 1024 * 1024);

        return rotatedPath;
    }
    #endregion Log Write Worker

    #region Cleanup Worker

    private async Task CleanupWorkerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                DeleteOldFiles();
                //DeleteEmptyDirectories(); // 1차원 구조로 셋업
                await Task.Delay(TimeSpan.FromHours(12), token); // 12시간마다 정리 작업 수행 
            }
            catch (TaskCanceledException)
            {
                // 정상 종료 시 무시
            }
        }
    }
    private void DeleteOldFiles()
    {
        foreach (var file in Directory.GetFiles(_baseDirectory, "*.log", SearchOption.AllDirectories))
        {
            var lastWriteTime = File.GetLastWriteTime(file);
            if ((DateTime.Now - lastWriteTime).TotalDays <= _retentionDays) continue; // 보존 기간 내의 파일은 건너뜀
            File.Delete(file);
        }
    }
    private void DeleteEmptyDirectories()
    {
        foreach (var dir in Directory.GetDirectories(_baseDirectory, "*", SearchOption.AllDirectories)
                                     .OrderByDescending(d => d.Length)) // 긴 경로 → 하위 폴더 먼저
        {
            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
            {
                Directory.Delete(dir);
            }
        }
    }
    #endregion Cleanup Worker

    #endregion Private Methods
}

