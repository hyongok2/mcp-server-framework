using System.Reflection;
using System.Text.Json;
using Micube.MCP.Core.Loader;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Services;

namespace Micube.MCP.Validator.Validators;

public class RuntimeValidator : IValidator
{
    private readonly IMcpLogger _logger;

    public RuntimeValidator()
    {
        _logger = new ValidatorLogger();
    }

    public string Name => "Runtime Validator";

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var report = new ValidationReport { Context = context };
        var startTime = DateTime.UtcNow;

        if (context.Level < ValidationLevel.Full)
        {
            report.AddInfo("Runtime", "RUN001", 
                "Runtime validation skipped (ValidationLevel < Full)");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        if (string.IsNullOrEmpty(context.DllPath) || string.IsNullOrEmpty(context.ManifestPath))
        {
            report.AddError("Runtime", "RUN002", 
                "Both DLL and Manifest paths are required for runtime validation");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        if (!File.Exists(context.DllPath) || !File.Exists(context.ManifestPath))
        {
            report.AddError("Runtime", "RUN003", 
                "DLL or Manifest file not found");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        try
        {
            // 어셈블리 로드
            var assembly = Assembly.LoadFrom(context.DllPath);
            var toolGroupTypes = assembly.GetTypes()
                .Where(t => typeof(IMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            if (toolGroupTypes.Count == 0)
            {
                report.AddError("Runtime", "RUN010", 
                    "No IMcpToolGroup implementation found");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // Manifest 로드
            ToolGroupMetadata? metadata = null;
            try
            {
                metadata = ToolGroupDescriptorParser.Parse(context.ManifestPath, _logger);
            }
            catch (Exception ex)
            {
                report.AddError("Runtime", "RUN011", 
                    $"Failed to parse manifest: {ex.Message}");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            if (metadata == null)
            {
                report.AddError("Runtime", "RUN012", "Could not load manifest metadata");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // 각 ToolGroup 타입에 대해 런타임 검증
            foreach (var type in toolGroupTypes)
            {
                var attr = type.GetCustomAttribute<McpToolGroupAttribute>();
                if (attr == null || attr.GroupName != metadata.GroupName) continue;

                await ValidateToolGroupRuntime(type, metadata, report);
            }

        }
        catch (Exception ex)
        {
            report.AddError("Runtime", "RUN999", 
                $"Unexpected error during runtime validation: {ex.Message}");
        }

        report.Duration = DateTime.UtcNow - startTime;
        return report;
    }

    private async Task ValidateToolGroupRuntime(Type toolGroupType, ToolGroupMetadata metadata, ValidationReport report)
    {
        IMcpToolGroup? instance = null;

        try
        {
            // 1. 인스턴스 생성 테스트
            var constructor = toolGroupType.GetConstructor(new[] { typeof(IMcpLogger) });
            if (constructor == null)
            {
                report.AddError("Runtime", "RUN020", 
                    $"Type '{toolGroupType.Name}' has no constructor(IMcpLogger)");
                return;
            }

            instance = (IMcpToolGroup)constructor.Invoke(new object[] { _logger });
            report.AddInfo("Runtime", "RUN100", 
                $"Successfully created instance of '{toolGroupType.Name}'");

            // 2. Configure 메서드 테스트
            try
            {
                instance.Configure(metadata.Config);
                report.AddInfo("Runtime", "RUN101", 
                    "Configure method executed successfully");
            }
            catch (Exception ex)
            {
                report.AddError("Runtime", "RUN021", 
                    $"Configure method failed: {ex.Message}");
            }

            // 3. Tool 메서드 실행 테스트
            await ValidateToolExecution(instance, toolGroupType, metadata, report);

            // 4. Dispose 패턴 확인
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                    report.AddInfo("Runtime", "RUN102", 
                        "IDisposable.Dispose executed successfully");
                }
                catch (Exception ex)
                {
                    report.AddWarning("Runtime", "RUN030", 
                        $"Dispose method threw exception: {ex.Message}");
                }
            }

        }
        catch (Exception ex)
        {
            report.AddError("Runtime", "RUN040", 
                $"Failed to create instance: {ex.Message}");
        }
    }

    private async Task ValidateToolExecution(IMcpToolGroup instance, Type toolGroupType, 
        ToolGroupMetadata metadata, ValidationReport report)
    {
        var methods = toolGroupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpToolAttribute>() != null).ToList();

        foreach (var method in toolMethods)
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>()!;
            var toolName = toolAttr.Name ?? method.Name;

            // Manifest에서 해당 Tool 정의 찾기
            var toolDescriptor = metadata.Tools?.FirstOrDefault(t => 
                string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));

            if (toolDescriptor == null)
            {
                report.AddWarning("Runtime", "RUN050", 
                    $"Tool '{toolName}' not found in manifest, skipping execution test");
                continue;
            }

            try
            {
                // 테스트용 파라미터 생성
                object? testParams = null;
                var parameters = method.GetParameters();
                
                if (parameters.Length > 0)
                {
                    var paramType = parameters[0].ParameterType;
                    testParams = CreateTestParameters(paramType, toolDescriptor);
                }

                // 메서드 실행
                var task = method.Invoke(instance, testParams != null ? new[] { testParams } : null) as Task;
                if (task != null)
                {
                    // 타임아웃 설정 (5초)
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        report.AddWarning("Runtime", "RUN051", 
                            $"Tool '{toolName}' execution timeout (>5s)");
                    }
                    else
                    {
                        // 결과 확인
                        if (task.IsFaulted)
                        {
                            report.AddError("Runtime", "RUN052", 
                                $"Tool '{toolName}' execution failed: {task.Exception?.GetBaseException().Message}");
                        }
                        else
                        {
                            // Task<ToolCallResult> 결과 확인
                            var resultProperty = task.GetType().GetProperty("Result");
                            if (resultProperty != null)
                            {
                                var result = resultProperty.GetValue(task);
                                if (result is ToolCallResult toolResult)
                                {
                                    if (toolResult.IsError)
                                    {
                                        report.AddInfo("Runtime", "RUN103", 
                                            $"Tool '{toolName}' returned error (expected for test parameters): {toolResult.Content?.FirstOrDefault()?.Text}");
                                    }
                                    else
                                    {
                                        report.AddInfo("Runtime", "RUN104", 
                                            $"Tool '{toolName}' executed successfully");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    report.AddError("Runtime", "RUN053", 
                        $"Tool '{toolName}' did not return a Task");
                }
            }
            catch (Exception ex)
            {
                report.AddWarning("Runtime", "RUN054", 
                    $"Tool '{toolName}' execution test failed: {ex.Message}",
                    "This may be expected if the tool requires specific runtime conditions");
            }
        }
    }

    private object CreateTestParameters(Type paramType, ToolDescriptor toolDescriptor)
    {
        var instance = Activator.CreateInstance(paramType);
        if (instance == null) return new object();

        // Required 파라미터에 대해 기본값 설정
        if (toolDescriptor.Parameters != null)
        {
            foreach (var param in toolDescriptor.Parameters.Where(p => p.Required))
            {
                var property = paramType.GetProperty(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var testValue = GetTestValue(param.Type, property.PropertyType);
                        property.SetValue(instance, testValue);
                    }
                    catch
                    {
                        // 테스트 값 설정 실패는 무시
                    }
                }
            }
        }

        return instance;
    }

    private object? GetTestValue(string manifestType, Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return manifestType.ToLowerInvariant() switch
        {
            "string" => "test_value",
            "int" or "integer" => 1,
            "number" => 1.0,
            "bool" or "boolean" => true,
            "array" => Array.CreateInstance(typeof(object), 0),
            _ => null
        };
    }
}

// 간단한 로거 구현
public class ValidatorLogger : IMcpLogger
{
    private readonly List<string> _logs = new();

    public void LogDebug(string message)
    {
        _logs.Add($"[DEBUG] {message}");
    }

    public void LogDebug(string message, object? requestId)
    {
        _logs.Add($"[DEBUG] [{requestId}] {message}");
    }

    public void LogInfo(string message)
    {
        _logs.Add($"[INFO] {message}");
    }

    public void LogInfo(string message, object? requestId)
    {
        _logs.Add($"[INFO] [{requestId}] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        _logs.Add($"[ERROR] {message}");
        if (ex != null)
        {
            _logs.Add($"[ERROR] Exception: {ex.Message}");
        }
    }

    public void LogError(string message, object? requestId, Exception? ex = null)
    {
        _logs.Add($"[ERROR] [{requestId}] {message}");
        if (ex != null)
        {
            _logs.Add($"[ERROR] Exception: {ex.Message}");
        }
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }

    public List<string> GetLogs() => _logs;
}