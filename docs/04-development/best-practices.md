# 🎯 모범 사례

> **프로덕션 수준의 안정적이고 효율적인 MCP 도구 개발 가이드**

이 문서는 실제 운영 환경에서 검증된 모범 사례들을 제시합니다. 성능, 보안, 유지보수성을 고려한 실무 중심의 가이드라인을 제공합니다.

## 🏗️ 설계 원칙

### **1. 단일 책임 원칙**
각 Tool Group은 명확하고 구체적인 기능 영역을 담당해야 합니다.

```csharp
// ✅ 좋은 예: 명확한 단일 책임
[McpToolGroup("FileOperations", "file-ops.json")]
public class FileOperationsGroup : BaseToolGroup
{
    [McpTool("ReadFile")]
    public Task<ToolCallResult> ReadFileAsync(Dictionary<string, object> parameters) { }
    
    [McpTool("WriteFile")]
    public Task<ToolCallResult> WriteFileAsync(Dictionary<string, object> parameters) { }
    
    [McpTool("DeleteFile")]
    public Task<ToolCallResult> DeleteFileAsync(Dictionary<string, object> parameters) { }
}

// ❌ 나쁜 예: 책임이 분산됨
[McpToolGroup("MixedOperations", "mixed.json")]
public class MixedOperationsGroup : BaseToolGroup
{
    [McpTool("ReadFile")]        // 파일 작업
    [McpTool("SendEmail")]       // 이메일 작업
    [McpTool("CalculateSum")]    // 수학 연산
    // 서로 관련 없는 기능들이 섞여 있음
}
```

### **2. 입력 검증 우선**
모든 매개변수는 사용 전에 철저히 검증해야 합니다.

```csharp
[McpTool("ProcessUserData")]
public async Task<ToolCallResult> ProcessUserDataAsync(Dictionary<string, object> parameters)
{
    // 1. 필수 매개변수 검증
    if (!parameters.TryGetValue("userId", out var userIdObj) || 
        !int.TryParse(userIdObj?.ToString(), out var userId) || 
        userId <= 0)
    {
        return ToolCallResult.Fail("Valid userId (positive integer) is required");
    }

    // 2. 선택적 매개변수 검증
    var email = parameters.TryGetValue("email", out var emailObj) ? emailObj?.ToString() : null;
    if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
    {
        return ToolCallResult.Fail("Invalid email format");
    }

    // 3. 비즈니스 규칙 검증
    if (await IsUserSuspendedAsync(userId))
    {
        return ToolCallResult.Fail("Cannot process data for suspended user");
    }

    // 검증 완료 후 실제 처리
    return await ProcessValidatedDataAsync(userId, email);
}

private static bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
```

### **3. 명시적 에러 처리**
예상 가능한 모든 오류 상황을 명시적으로 처리합니다.

```csharp
[McpTool("DownloadFile")]
public async Task<ToolCallResult> DownloadFileAsync(Dictionary<string, object> parameters)
{
    try
    {
        var url = parameters["url"]?.ToString();
        if (string.IsNullOrEmpty(url))
        {
            return ToolCallResult.Fail("URL parameter is required");
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var response = await httpClient.GetAsync(url);
        
        // HTTP 상태 코드별 세분화된 처리
        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ProcessSuccessfulDownload(response),
            HttpStatusCode.NotFound => ToolCallResult.Fail("File not found (404)"),
            HttpStatusCode.Forbidden => ToolCallResult.Fail("Access denied (403)"),
            HttpStatusCode.Unauthorized => ToolCallResult.Fail("Authentication required (401)"),
            _ => ToolCallResult.Fail($"Download failed: {response.StatusCode}")
        };
    }
    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
    {
        return ToolCallResult.Fail("Download timed out after 5 minutes");
    }
    catch (HttpRequestException ex)
    {
        Logger.LogError($"Network error during download: {ex.Message}", ex);
        return ToolCallResult.Fail("Network error occurred");
    }
    catch (Exception ex)
    {
        Logger.LogError($"Unexpected error during download: {ex.Message}", ex);
        return ToolCallResult.Fail("An unexpected error occurred");
    }
}
```

## 🚀 성능 최적화

### **1. 비동기 패턴 활용**
```csharp
[McpTool("ProcessMultipleFiles")]
public async Task<ToolCallResult> ProcessMultipleFilesAsync(Dictionary<string, object> parameters)
{
    var filePaths = GetFilePathsFromParameters(parameters);
    var maxConcurrency = GetMaxConcurrency(); // 설정에서 읽기
    
    // SemaphoreSlim을 사용한 동시성 제어
    using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    
    var tasks = filePaths.Select(async filePath =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await ProcessSingleFileAsync(filePath);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var results = await Task.WhenAll(tasks);
    
    return ToolCallResult.SuccessStructured(new
    {
        processedFiles = results.Length,
        successCount = results.Count(r => r.Success),
        failureCount = results.Count(r => !r.Success),
        results = results
    });
}
```

### **2. 메모리 효율적 스트림 처리**
```csharp
[McpTool("ProcessLargeFile")]
public async Task<ToolCallResult> ProcessLargeFileAsync(Dictionary<string, object> parameters)
{
    var filePath = parameters["filePath"]?.ToString();
    var processedLines = 0;
    var errors = new List<string>();

    try
    {
        // 대용량 파일을 스트림으로 처리 (메모리 절약)
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream, bufferSize: 65536); // 64KB 버퍼

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            try
            {
                await ProcessLineAsync(line);
                processedLines++;
                
                // 진행률 로깅 (매 1000줄마다)
                if (processedLines % 1000 == 0)
                {
                    Logger.LogDebug($"Processed {processedLines} lines");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Line {processedLines + 1}: {ex.Message}");
                
                // 에러가 너무 많으면 중단
                if (errors.Count > 100)
                {
                    Logger.LogError("Too many errors, stopping processing");
                    break;
                }
            }
        }

        return ToolCallResult.SuccessStructured(new
        {
            processedLines,
            errorCount = errors.Count,
            errors = errors.Take(10).ToArray() // 처음 10개 에러만 반환
        });
    }
    catch (Exception ex)
    {
        Logger.LogError($"Failed to process file: {ex.Message}", ex);
        return ToolCallResult.Fail($"File processing failed: {ex.Message}");
    }
}
```

### **3. 캐싱 전략**
```csharp
public class CachedApiToolGroup : BaseToolGroup
{
    private readonly MemoryCache _cache = new();
    private readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromMinutes(15);

    [McpTool("GetUserProfile")]
    public async Task<ToolCallResult> GetUserProfileAsync(Dictionary<string, object> parameters)
    {
        var userId = parameters["userId"]?.ToString();
        var cacheKey = $"user_profile:{userId}";

        // 캐시에서 먼저 확인
        if (_cache.TryGetValue(cacheKey, out var cachedProfile))
        {
            Logger.LogDebug($"Cache hit for user {userId}");
            return ToolCallResult.SuccessStructured(cachedProfile);
        }

        // 캐시 미스 시 API 호출
        Logger.LogDebug($"Cache miss for user {userId}, fetching from API");
        var profile = await FetchUserProfileFromApiAsync(userId);

        //
```