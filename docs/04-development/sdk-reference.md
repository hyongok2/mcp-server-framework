# 📚 SDK 참조

> **Micube.MCP.SDK 개발 SDK 완전 참조 가이드**

MCP Server Framework의 개발 SDK는 강력하고 직관적인 도구 개발을 위한 모든 구성 요소를 제공합니다. 이 문서는 SDK의 모든 인터페이스, 클래스, 속성에 대한 상세한 참조를 제공합니다.

## 🎯 SDK 구조 개요

```
Micube.MCP.SDK
├── Abstracts/           # 기본 클래스
│   └── BaseToolGroup
├── Attributes/          # 어트리뷰트
│   ├── McpToolGroupAttribute
│   └── McpToolAttribute
├── Interfaces/          # 인터페이스
│   ├── IMcpToolGroup
│   └── IMcpLogger
├── Models/             # 데이터 모델
│   ├── ToolCallResult
│   └── ToolContent
└── Exceptions/         # 예외 클래스
    ├── McpException
    └── McpErrorCodes
```

## 🏗️ 핵심 인터페이스

### **IMcpToolGroup**
모든 Tool Group이 구현해야 하는 기본 인터페이스입니다.

```csharp
namespace Micube.MCP.SDK.Interfaces;

public interface IMcpToolGroup
{
    /// <summary>
    /// Tool Group의 고유 식별자
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// 지정된 도구를 실행합니다
    /// </summary>
    /// <param name="toolName">실행할 도구명</param>
    /// <param name="parameters">매개변수 딕셔너리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>도구 실행 결과</returns>
    Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tool Group 설정을 초기화합니다
    /// </summary>
    /// <param name="config">JSON 설정 요소</param>
    void Configure(JsonElement? config);
}
```

### **IMcpLogger**
로깅 기능을 제공하는 인터페이스입니다.

```csharp
namespace Micube.MCP.SDK.Interfaces;

public interface IMcpLogger
{
    // 기본 로깅 메서드
    void LogInfo(string message);
    void LogDebug(string message);
    void LogError(string message, Exception? ex = null);
    
    // 요청 ID 포함 로깅 메서드
    void LogInfo(string message, object? requestId);
    void LogDebug(string message, object? requestId);
    void LogError(string message, object? requestId, Exception? ex = null);
    
    // 종료 메서드
    Task ShutdownAsync();
}
```

**사용 예시:**
```csharp
public class MyToolGroup : BaseToolGroup
{
    public MyToolGroup(IMcpLogger logger) : base(logger) { }

    [McpTool("ProcessData")]
    public async Task<ToolCallResult> ProcessDataAsync(Dictionary<string, object> parameters)
    {
        Logger.LogInfo("Starting data processing");
        
        try
        {
            // 처리 로직
            Logger.LogDebug($"Processing {parameters.Count} parameters");
            
            return ToolCallResult.Success("Processing completed");
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing failed", ex);
            return ToolCallResult.Fail($"Error: {ex.Message}");
        }
    }
}
```

## 🎨 기본 클래스

### **BaseToolGroup**
모든 Tool Group의 기본 구현을 제공하는 추상 클래스입니다.

```csharp
namespace Micube.MCP.SDK.Abstracts;

public abstract class BaseToolGroup : IMcpToolGroup
{
    /// <summary>
    /// Tool Group의 고유 이름 (구현 필수)
    /// </summary>
    public abstract string GroupName { get; }
    
    /// <summary>
    /// 로거 인스턴스 (하위 클래스에서 사용 가능)
    /// </summary>
    protected IMcpLogger Logger { get; }
    
    /// <summary>
    /// 원시 설정 데이터 (OnConfigure 호출 후 사용 가능)
    /// </summary>
    protected JsonElement? RawConfig { get; private set; }

    /// <summary>
    /// 생성자 - IMcpLogger 주입 필수
    /// </summary>
    protected BaseToolGroup(IMcpLogger logger);

    /// <summary>
    /// 도구 호출 메서드 (자동 구현됨)
    /// </summary>
    public async Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 설정 초기화 메서드 (자동 호출됨)
    /// </summary>
    public void Configure(JsonElement? config);

    /// <summary>
    /// 설정 처리 로직 (구현 선택사항)
    /// </summary>
    protected abstract void OnConfigure(JsonElement? config);
}
```

**구현 예시:**
```csharp
[McpToolGroup("DataProcessor", "data-processor.json")]
public class DataProcessorGroup : BaseToolGroup
{
    public override string GroupName { get; } = "DataProcessor";
    
    private string? _connectionString;
    private int _timeoutSeconds = 30;

    public DataProcessorGroup(IMcpLogger logger) : base(logger) { }

    protected override void OnConfigure(JsonElement? config)
    {
        if (config.HasValue)
        {
            // 설정에서 연결 문자열 읽기
            if (config.Value.TryGetProperty("connectionString", out var connElement))
            {
                _connectionString = connElement.GetString();
                Logger.LogInfo($"Connection string configured");
            }

            // 타임아웃 설정
            if (config.Value.TryGetProperty("timeoutSeconds", out var timeoutElement))
            {
                _timeoutSeconds = timeoutElement.GetInt32();
                Logger.LogInfo($"Timeout set to {_timeoutSeconds} seconds");
            }
        }
    }

    [McpTool("QueryData")]
    public async Task<ToolCallResult> QueryDataAsync(Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return ToolCallResult.Fail("Connection string not configured");
        }

        // 쿼리 실행 로직...
        return ToolCallResult.Success("Query executed successfully");
    }
}
```

## 🏷️ 어트리뷰트

### **McpToolGroupAttribute**
Tool Group 클래스를 표시하는 어트리뷰트입니다.

```csharp
namespace Micube.MCP.SDK.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class McpToolGroupAttribute : Attribute
{
    /// <summary>
    /// Tool Group의 고유 이름
    /// </summary>
    public string GroupName { get; }
    
    /// <summary>
    /// Manifest 파일 경로 (상대 경로)
    /// </summary>
    public string ManifestPath { get; }
    
    /// <summary>
    /// Tool Group 설명 (선택사항)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="groupName">그룹 고유 이름</param>
    /// <param name="manifestPath">Manifest 파일 경로 (.json으로 끝나야 함)</param>
    /// <param name="description">설명 (선택사항)</param>
    public McpToolGroupAttribute(string groupName, string manifestPath, string? description = null);
}
```

**사용 예시:**
```csharp
// 기본 사용
[McpToolGroup("FileTools", "file-tools.json")]
public class FileToolGroup : BaseToolGroup { }

// 설명 포함
[McpToolGroup("DatabaseTools", "db-tools.json", "Database operation tools")]
public class DatabaseToolGroup : BaseToolGroup { }

// 하위 디렉토리의 Manifest
[McpToolGroup("AdvancedTools", "advanced/advanced-tools.json")]
public class AdvancedToolGroup : BaseToolGroup { }
```

### **McpToolAttribute**
도구 메서드를 표시하는 어트리뷰트입니다.

```csharp
namespace Micube.MCP.SDK.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// 도구의 고유 이름
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="name">도구 고유 이름</param>
    public McpToolAttribute(string name);
}
```

**사용 예시:**
```csharp
public class ExampleToolGroup : BaseToolGroup
{
    [McpTool("ReadFile")]
    public async Task<ToolCallResult> ReadFileAsync(Dictionary<string, object> parameters)
    {
        // 구현
    }

    [McpTool("WriteFile")]
    public async Task<ToolCallResult> WriteFileAsync(Dictionary<string, object> parameters)
    {
        // 구현
    }

    // 도구명과 메서드명이 다를 수 있음
    [McpTool("ListDirectory")]
    public async Task<ToolCallResult> GetDirectoryListingAsync(Dictionary<string, object> parameters)
    {
        // 구현
    }
}
```

## 📦 데이터 모델

### **ToolCallResult**
도구 실행 결과를 나타내는 클래스입니다.

```csharp
namespace Micube.MCP.SDK.Models;

public class ToolCallResult
{
    /// <summary>
    /// 결과 콘텐츠 목록
    /// </summary>
    [JsonProperty("content")]
    public List<ToolContent> Content { get; set; } = new();
    
    /// <summary>
    /// 에러 여부
    /// </summary>
    [JsonProperty("isError")]
    public bool IsError { get; set; } = false;

    // 정적 팩토리 메서드들
    
    /// <summary>
    /// 성공 결과 생성 (텍스트)
    /// </summary>
    public static ToolCallResult Success(params string[] messages);
    
    /// <summary>
    /// 실패 결과 생성
    /// </summary>
    public static ToolCallResult Fail(string message);
}
```

**사용 예시:**
```csharp
// 단순 텍스트 성공
return ToolCallResult.Success("파일을 성공적으로 읽었습니다.");

// 여러 메시지
return ToolCallResult.Success(
    "처리 완료",
    $"총 {count}개 항목 처리됨",
    $"소요 시간: {elapsed}ms"
);

// 구조화된 데이터
var result = new
{
    files = fileList,
    totalSize = totalBytes,
    count = fileList.Count
};
return result;

// 스키마 포함 구조화된 데이터
var schema = new
{
    type = "object",
    properties = new
    {
        files = new { type = "array" },
        totalSize = new { type = "number" },
        count = new { type = "integer" }
    }
};
return schema;

// 에러
return ToolCallResult.Fail("파일을 찾을 수 없습니다.");
```

### **ToolContent**
도구 결과의 개별 콘텐츠를 나타내는 클래스입니다.

```csharp
namespace Micube.MCP.SDK.Models;

public class ToolContent
{
    /// <summary>
    /// 콘텐츠 타입 (text, image, code 등)
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// 텍스트 콘텐츠
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }

    // 생성자들
    public ToolContent();
    public ToolContent(string type, string? text);

}
```

**사용 예시:**
```csharp
// 직접 생성 (일반적으로 필요하지 않음)
var contents = new List<ToolContent>
{
    new ToolContent("text", "처리 결과"),
    new ToolContent(new { status = "success", count = 10 })
};

return new ToolCallResult { Content = contents, IsError = false };
```

## 🚨 예외 처리

### **McpException**
MCP 관련 예외를 나타내는 클래스입니다.

```csharp
namespace Micube.MCP.SDK.Exceptions;

public class McpException : Exception
{
    public McpException(string message) : base(message) { }
    public McpException(string message, Exception innerException) : base(message, innerException) { }
}
```

### **McpErrorCodes**
표준 JSON-RPC 에러 코드를 정의하는 클래스입니다.

```csharp
namespace Micube.MCP.SDK.Exceptions;

public static class McpErrorCodes
{
    public const int PARSE_ERROR = -32700;      // JSON 파싱 오류
    public const int INVALID_REQUEST = -32600;  // 잘못된 요청
    public const int METHOD_NOT_FOUND = -32601; // 메서드를 찾을 수 없음
    public const int INVALID_PARAMS = -32602;   // 잘못된 매개변수
    public const int INTERNAL_ERROR = -32603;   // 내부 오류
}
```

**사용 예시:**
```csharp
[McpTool("ValidateData")]
public async Task<ToolCallResult> ValidateDataAsync(Dictionary<string, object> parameters)
{
    try
    {
        if (!parameters.ContainsKey("data"))
        {
            throw new McpException("Required parameter 'data' is missing");
        }

        // 검증 로직...
        
        return ToolCallResult.Success("Validation passed");
    }
    catch (McpException ex)
    {
        Logger.LogError($"Validation failed: {ex.Message}", ex);
        return ToolCallResult.Fail(ex.Message);
    }
    catch (Exception ex)
    {
        Logger.LogError($"Unexpected error: {ex.Message}", ex);
        return ToolCallResult.Fail("An unexpected error occurred");
    }
}
```

## 🔧 고급 패턴

### **비동기 처리**
```csharp
[McpTool("ProcessLargeFile")]
public async Task<ToolCallResult> ProcessLargeFileAsync(Dictionary<string, object> parameters)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    
    try
    {
        var filePath = parameters["path"].ToString();
        var result = await ProcessFileWithTimeoutAsync(filePath, cts.Token);
        
        return ToolCallResult.Success($"Processed {result.ProcessedLines} lines");
    }
    catch (OperationCanceledException)
    {
        return ToolCallResult.Fail("Processing timed out");
    }
}

private async Task<ProcessResult> ProcessFileWithTimeoutAsync(string filePath, CancellationToken cancellationToken)
{
    // 취소 토큰을 활용한 비동기 처리
    return await Task.Run(() => 
    {
        // 실제 처리 로직
        cancellationToken.ThrowIfCancellationRequested();
        return new ProcessResult { ProcessedLines = 1000 };
    }, cancellationToken);
}
```

### **리소스 관리**
```csharp
[McpTool("DatabaseQuery")]
public async Task<object> DatabaseQueryAsync(Dictionary<string, object> parameters)
{
    using var connection = new SqlConnection(_connectionString);
    using var command = new SqlCommand(parameters["query"].ToString(), connection);
    
    try
    {
        await connection.OpenAsync();
        
        using var reader = await command.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object>>();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return new { rows = results, count = results.Count };
    }
    catch (SqlException ex)
    {
        Logger.LogError($"Database error: {ex.Message}", ex);
        return ToolCallResult.Fail($"Database error: {ex.Message}");
    }
}
```

### **설정 기반 동작**
```csharp
public class ConfigurableToolGroup : BaseToolGroup
{
    private readonly Dictionary<string, string> _endpoints = new();
    private readonly Dictionary<string, string> _apiKeys = new();
    private int _retryCount = 3;

    protected override void OnConfigure(JsonElement? config)
    {
        if (!config.HasValue) return;

        // API 엔드포인트 설정
        if (config.Value.TryGetProperty("endpoints", out var endpointsElement))
        {
            foreach (var endpoint in endpointsElement.EnumerateObject())
            {
                _endpoints[endpoint.Name] = endpoint.Value.GetString() ?? "";
            }
        }

        // API 키 설정
        if (config.Value.TryGetProperty("apiKeys", out var apiKeysElement))
        {
            foreach (var apiKey in apiKeysElement.EnumerateObject())
            {
                _apiKeys[apiKey.Name] = apiKey.Value.GetString() ?? "";
            }
        }

        // 재시도 횟수 설정
        if (config.Value.TryGetProperty("retryCount", out var retryElement))
        {
            _retryCount = retryElement.GetInt32();
        }

        Logger.LogInfo($"Configured {_endpoints.Count} endpoints and {_apiKeys.Count} API keys");
    }

    [McpTool("CallExternalAPI")]
    public async Task<object> CallExternalAPIAsync(Dictionary<string, object> parameters)
    {
        var serviceName = parameters["service"].ToString();
        
        if (!_endpoints.TryGetValue(serviceName, out var endpoint))
        {
            return ToolCallResult.Fail($"Service '{serviceName}' not configured");
        }

        if (!_apiKeys.TryGetValue(serviceName, out var apiKey))
        {
            return ToolCallResult.Fail($"API key for '{serviceName}' not configured");
        }

        // API 호출 로직 with retry
        for (int attempt = 1; attempt <= _retryCount; attempt++)
        {
            try
            {
                var result = await CallAPIWithAuthAsync(endpoint, apiKey, parameters);
                return result;
            }
            catch (HttpRequestException ex) when (attempt < _retryCount)
            {
                Logger.LogDebug($"API call attempt {attempt} failed, retrying: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
            }
        }

        return ToolCallResult.Fail($"API call failed after {_retryCount} attempts");
    }
}
```

---

**다음**: [모범 사례](best-practices.md) - 개발 베스트 프랙티스 →

**이전**: [← 프롬프트 템플릿](prompt-templates.md)