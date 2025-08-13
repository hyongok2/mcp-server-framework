using System.Reflection;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Services;

namespace Micube.MCP.Validator.Validators;

public class DllValidator : IValidator
{
    private readonly FileLogger? _logger;
    
    public string Name => "DLL Validator";

    public DllValidator(FileLogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger?.LogValidationStart(Name, context.DllPath);
        
        try
        {
            var report = new ValidationReport { Context = context };

            if (string.IsNullOrEmpty(context.DllPath))
            {
                _logger?.LogError(Name, "DLL path not specified");
                report.AddError("DLL", "DLL001", "DLL path is not specified");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            _logger?.LogInfo(Name, "Validating DLL", $"File: {Path.GetFileName(context.DllPath)}");

            // 파일 존재 여부 확인
            if (!File.Exists(context.DllPath))
            {
                _logger?.LogError(Name, "DLL file not found", context.DllPath);
                report.AddError("DLL", "DLL002", $"DLL file not found: {context.DllPath}");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            report.Statistics.ValidatedFiles.Add(context.DllPath);
            _logger?.LogInfo(Name, "DLL file exists", $"Path: {context.DllPath}");

            // 어셈블리 로드
            Assembly assembly;
            try
            {
                _logger?.LogInfo(Name, "Loading assembly", Path.GetFileName(context.DllPath));
                assembly = Assembly.LoadFrom(context.DllPath);
                
                var assemblyName = assembly.GetName();
                _logger?.LogInfo(Name, "Assembly loaded successfully", 
                    $"Name: {assemblyName.Name}, Version: {assemblyName.Version}");
                    
                report.AddInfo("DLL", "DLL100", $"Successfully loaded assembly: {assemblyName.Name} v{assemblyName.Version}");
            }
            catch (BadImageFormatException ex)
            {
                _logger?.LogError(Name, "Invalid .NET assembly format", context.DllPath, ex);
                report.AddError("DLL", "DLL003", "Invalid .NET assembly format",
                    "The file is not a valid .NET assembly or has incompatible architecture");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }
            catch (Exception ex)
            {
                _logger?.LogError(Name, "Failed to load assembly", context.DllPath, ex);
                report.AddError("DLL", "DLL004", $"Failed to load assembly: {ex.Message}");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // IMcpToolGroup 구현 찾기
            List<Type> toolGroupTypes;
            try
            {
                toolGroupTypes = assembly.GetTypes()
                    .Where(t => typeof(IMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 종속 DLL이 없어서 발생하는 오류는 경고로 처리
                report.AddWarning("DLL", "DLL005", 
                    $"Could not load all types from assembly: {ex.Message}",
                    "This assembly may be a dependency DLL, not a MCP tool DLL");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }
            catch (Exception ex)
            {
                // 기타 타입 로드 오류
                report.AddWarning("DLL", "DLL006", 
                    $"Failed to analyze assembly types: {ex.Message}",
                    "This assembly may not be a MCP tool DLL");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            if (toolGroupTypes.Count == 0)
            {
                report.AddWarning("DLL", "DLL010", "No IMcpToolGroup implementation found",
                    "This assembly does not contain MCP tool implementations (may be a dependency DLL)");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // 각 ToolGroup 검증
            foreach (var type in toolGroupTypes)
            {
                ValidateToolGroupType(type, report, context);
            }

            // 의존성 검증
            ValidateDependencies(assembly, report);

            // SDK 버전 호환성 체크
            ValidateSdkCompatibility(assembly, report);

            report.Duration = DateTime.UtcNow - startTime;
            _logger?.LogValidationEnd(Name, context.DllPath, report.Duration);
            _logger?.LogInfo(Name, "DLL validation completed", 
                $"Issues: {report.Issues.Count}, Errors: {report.Issues.Count(i => i.Severity == IssueSeverity.Error)}");

            return await Task.FromResult(report);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogCritical(Name, "Unexpected error during DLL validation", 
                $"DLL: {Path.GetFileName(context.DllPath ?? "unknown")}, Duration: {duration.TotalMilliseconds:F0}ms", ex);
            
            var errorReport = new ValidationReport { Context = context };
            errorReport.AddError("DLL", "DLL999", $"Unexpected error during validation: {ex.Message}", ex.ToString());
            errorReport.Duration = duration;
            
            return await Task.FromResult(errorReport);
        }
    }

    private void ValidateToolGroupType(Type type, ValidationReport report, ValidationContext context)
    {
        var typeName = type.FullName ?? type.Name;

        // McpToolGroupAttribute 검증
        var attr = type.GetCustomAttribute<McpToolGroupAttribute>();
        if (attr == null)
        {
            report.AddError("DLL", "DLL020", 
                $"Type '{typeName}' is missing [McpToolGroup] attribute",
                "All IMcpToolGroup implementations must have the McpToolGroup attribute");
            return;
        }

        // Attribute 필수 속성 검증
        if (string.IsNullOrWhiteSpace(attr.GroupName))
        {
            report.AddError("DLL", "DLL021", 
                $"Type '{typeName}': McpToolGroup.GroupName is empty");
        }
        else
        {
            // 메타데이터에 GroupName 저장
            context.Metadata[$"DLL.GroupName.{type.Name}"] = attr.GroupName;
            report.AddInfo("DLL", "DLL101", 
                $"Found tool group: '{attr.GroupName}' in type '{type.Name}'");
        }

        if (string.IsNullOrWhiteSpace(attr.ManifestPath))
        {
            report.AddError("DLL", "DLL022", 
                $"Type '{typeName}': McpToolGroup.ManifestPath is empty");
        }

        // 생성자 검증
        var constructor = type.GetConstructor(new[] { typeof(IMcpLogger) });
        if (constructor == null)
        {
            report.AddError("DLL", "DLL023", 
                $"Type '{typeName}' is missing required constructor(IMcpLogger)",
                "ToolGroup classes must have a public constructor that accepts IMcpLogger parameter");
        }

        // Tool 메서드 검증
        ValidateToolMethods(type, report);

        // BaseToolGroup 상속 확인 (선택사항)
        if (type.BaseType?.Name == "BaseToolGroup")
        {
            report.AddInfo("DLL", "DLL102", 
                $"Type '{type.Name}' extends BaseToolGroup (recommended)");
        }
        else if (context.StrictMode)
        {
            report.AddWarning("DLL", "DLL024",
                $"Type '{type.Name}' does not extend BaseToolGroup",
                "Consider extending BaseToolGroup for standard implementations");
        }
    }

    private void ValidateToolMethods(Type type, ValidationReport report)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var toolMethods = methods.Where(m => m.GetCustomAttribute<McpToolAttribute>() != null).ToList();

        if (toolMethods.Count == 0)
        {
            report.AddWarning("DLL", "DLL030", 
                $"Type '{type.Name}' has no methods with [McpTool] attribute");
            return;
        }

        var toolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var method in toolMethods)
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>()!;
            var toolName = toolAttr.Name ?? method.Name;

            // 중복 이름 검사
            if (!toolNames.Add(toolName))
            {
                report.AddError("DLL", "DLL031", 
                    $"Duplicate tool name '{toolName}' in type '{type.Name}'");
            }

            // 반환 타입 검증 - BaseToolGroup에서 모든 Task 타입을 처리할 수 있음
            var returnType = method.ReturnType;
            bool isValidReturnType = false;

            if (returnType == typeof(Task))
            {
                // Task (void) - 기본 작업 완료 반환
                isValidReturnType = true;
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<T> - BaseToolGroup이 모든 타입을 ToolCallResult로 변환 가능
                isValidReturnType = true;
            }

            if (!isValidReturnType)
            {
                report.AddError("DLL", "DLL032",
                    $"Tool method '{method.Name}' has invalid return type",
                    "Tool methods must return Task or Task<T>");
            }

            // 파라미터 검증
            var parameters = method.GetParameters();
            if (parameters.Length > 1)
            {
                report.AddWarning("DLL", "DLL033",
                    $"Tool method '{method.Name}' has {parameters.Length} parameters",
                    "Tool methods typically accept a single parameter object");
            }

            report.AddInfo("DLL", "DLL103", 
                $"Found tool method: '{toolName}' ({method.Name})");
        }
    }

    private void ValidateDependencies(Assembly assembly, ValidationReport report)
    {
        try
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            
            // 필수 의존성 확인
            var hasMcpSdk = referencedAssemblies.Any(a => a.Name == "Micube.MCP.SDK");
            if (!hasMcpSdk)
            {
                report.AddError("DLL", "DLL040",
                    "Missing reference to Micube.MCP.SDK",
                    "The assembly must reference Micube.MCP.SDK");
            }

            // 추가 의존성 정보
            var externalDeps = referencedAssemblies
                .Where(a => !a.Name?.StartsWith("System") ?? false)
                .Where(a => !a.Name?.StartsWith("Microsoft") ?? false)
                .Where(a => a.Name != "Micube.MCP.SDK")
                .ToList();

            if (externalDeps.Count > 0)
            {
                var depList = string.Join(", ", externalDeps.Select(a => $"{a.Name} v{a.Version}"));
                report.AddInfo("DLL", "DLL104", 
                    $"External dependencies: {depList}");
            }
        }
        catch (Exception ex)
        {
            report.AddWarning("DLL", "DLL041",
                $"Could not analyze dependencies: {ex.Message}");
        }
    }

    private void ValidateSdkCompatibility(Assembly assembly, ValidationReport report)
    {
        try
        {
            var sdkReference = assembly.GetReferencedAssemblies()
                .FirstOrDefault(a => a.Name == "Micube.MCP.SDK");

            if (sdkReference != null)
            {
                var currentSdkVersion = typeof(IMcpToolGroup).Assembly.GetName().Version;
                var referencedSdkVersion = sdkReference.Version;

                if (currentSdkVersion != null && referencedSdkVersion != null)
                {
                    if (referencedSdkVersion.Major != currentSdkVersion.Major)
                    {
                        report.AddWarning("DLL", "DLL050",
                            $"SDK version mismatch - DLL uses v{referencedSdkVersion}, current is v{currentSdkVersion}",
                            "Major version differences may cause compatibility issues");
                    }
                    else if (referencedSdkVersion < currentSdkVersion)
                    {
                        report.AddInfo("DLL", "DLL105",
                            $"DLL uses older SDK version (v{referencedSdkVersion}), current is v{currentSdkVersion}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            report.AddInfo("DLL", "DLL051",
                $"Could not verify SDK compatibility: {ex.Message}");
        }
    }
}