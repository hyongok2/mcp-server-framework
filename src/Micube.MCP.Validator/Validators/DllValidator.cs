using System.Reflection;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Services;
using Micube.MCP.Validator.Constants;

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

            if (!ValidateDllPath(context.DllPath, report, startTime))
            {
                return report;
            }

            var assembly = LoadAssembly(context.DllPath!, report, startTime);
            if (assembly == null)
            {
                return report;
            }

            var toolGroupTypes = GetToolGroupTypes(assembly, report, startTime);
            if (toolGroupTypes == null)
            {
                return report;
            }

            ValidateToolGroups(toolGroupTypes, report, context);
            ValidateDependencies(assembly, report);
            ValidateSdkCompatibility(assembly, report);

            CompleteValidation(report, startTime, context.DllPath!);

            return await Task.FromResult(report);
        }
        catch (Exception ex)
        {
            return HandleValidationError(ex, context, startTime);
        }
    }

    private bool ValidateDllPath(string? dllPath, ValidationReport report, DateTime startTime)
    {
        if (string.IsNullOrEmpty(dllPath))
        {
            _logger?.LogError(Name, "DLL path not specified");
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.PathNotSpecified, "DLL path is not specified");
            report.Duration = DateTime.UtcNow - startTime;
            return false;
        }

        _logger?.LogInfo(Name, "Validating DLL", $"File: {Path.GetFileName(dllPath)}");

        if (!File.Exists(dllPath))
        {
            _logger?.LogError(Name, "DLL file not found", dllPath);
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.FileNotFound, $"DLL file not found: {dllPath}");
            report.Duration = DateTime.UtcNow - startTime;
            return false;
        }

        report.Statistics.ValidatedFiles.Add(dllPath);
        _logger?.LogInfo(Name, "DLL file exists", $"Path: {dllPath}");

        return true;
    }

    private Assembly? LoadAssembly(string dllPath, ValidationReport report, DateTime startTime)
    {
        try
        {
            _logger?.LogInfo(Name, "Loading assembly", Path.GetFileName(dllPath));
            var assembly = Assembly.LoadFrom(dllPath);
            
            var assemblyName = assembly.GetName();
            _logger?.LogInfo(Name, "Assembly loaded successfully", 
                $"Name: {assemblyName.Name}, Version: {assemblyName.Version}");
                
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.AssemblyLoaded, 
                $"Successfully loaded assembly: {assemblyName.Name} v{assemblyName.Version}");

            return assembly;
        }
        catch (BadImageFormatException ex)
        {
            _logger?.LogError(Name, "Invalid .NET assembly format", dllPath, ex);
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.InvalidAssemblyFormat, "Invalid .NET assembly format",
                "The file is not a valid .NET assembly or has incompatible architecture");
            report.Duration = DateTime.UtcNow - startTime;
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(Name, "Failed to load assembly", dllPath, ex);
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.LoadFailed, $"Failed to load assembly: {ex.Message}");
            report.Duration = DateTime.UtcNow - startTime;
            return null;
        }
    }

    private List<Type>? GetToolGroupTypes(Assembly assembly, ValidationReport report, DateTime startTime)
    {
        try
        {
            var toolGroupTypes = assembly.GetTypes()
                .Where(t => typeof(IMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            if (toolGroupTypes.Count == 0)
            {
                report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.NoToolGroupImplementation, 
                    "No IMcpToolGroup implementation found",
                    "This assembly does not contain MCP tool implementations (may be a dependency DLL)");
                report.Duration = DateTime.UtcNow - startTime;
                return null;
            }

            return toolGroupTypes;
        }
        catch (ReflectionTypeLoadException ex)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.TypeLoadWarning, 
                $"Could not load all types from assembly: {ex.Message}",
                "This assembly may be a dependency DLL, not a MCP tool DLL");
            report.Duration = DateTime.UtcNow - startTime;
            return null;
        }
        catch (Exception ex)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.AnalysisWarning, 
                $"Failed to analyze assembly types: {ex.Message}",
                "This assembly may not be a MCP tool DLL");
            report.Duration = DateTime.UtcNow - startTime;
            return null;
        }
    }

    private void ValidateToolGroups(List<Type> toolGroupTypes, ValidationReport report, ValidationContext context)
    {
        foreach (var type in toolGroupTypes)
        {
            ValidateToolGroupType(type, report, context);
        }
    }

    private void CompleteValidation(ValidationReport report, DateTime startTime, string dllPath)
    {
        report.Duration = DateTime.UtcNow - startTime;
        _logger?.LogValidationEnd(Name, dllPath, report.Duration);
        _logger?.LogInfo(Name, "DLL validation completed", 
            $"Issues: {report.Issues.Count}, Errors: {report.Issues.Count(i => i.Severity == IssueSeverity.Error)}");
    }

    private ValidationReport HandleValidationError(Exception ex, ValidationContext context, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;
        _logger?.LogCritical(Name, "Unexpected error during DLL validation", 
            $"DLL: {Path.GetFileName(context.DllPath ?? "unknown")}, Duration: {duration.TotalMilliseconds:F0}ms", ex);
        
        var errorReport = new ValidationReport { Context = context };
        errorReport.AddError("DLL", ValidationConstants.ErrorCodes.Dll.UnexpectedError, 
            $"Unexpected error during validation: {ex.Message}", ex.ToString());
        errorReport.Duration = duration;
        
        return errorReport;
    }

    private void ValidateToolGroupType(Type type, ValidationReport report, ValidationContext context)
    {
        var typeName = type.FullName ?? type.Name;

        // McpToolGroupAttribute 검증
        var attr = type.GetCustomAttribute<McpToolGroupAttribute>();
        if (attr == null)
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.MissingMcpToolGroupAttribute, 
                $"Type '{typeName}' is missing [McpToolGroup] attribute",
                "All IMcpToolGroup implementations must have the McpToolGroup attribute");
            return;
        }

        // Attribute 필수 속성 검증
        if (string.IsNullOrWhiteSpace(attr.GroupName))
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.EmptyGroupName, 
                $"Type '{typeName}': McpToolGroup.GroupName is empty");
        }
        else
        {
            // 메타데이터에 GroupName 저장
            context.Metadata[$"DLL.GroupName.{type.Name}"] = attr.GroupName;
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.ToolGroupFound, 
                $"Found tool group: '{attr.GroupName}' in type '{type.Name}'");
        }

        if (string.IsNullOrWhiteSpace(attr.ManifestPath))
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.EmptyManifestPath, 
                $"Type '{typeName}': McpToolGroup.ManifestPath is empty");
        }

        // 생성자 검증
        var constructor = type.GetConstructor(new[] { typeof(IMcpLogger) });
        if (constructor == null)
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.MissingRequiredConstructor, 
                $"Type '{typeName}' is missing required constructor(IMcpLogger)",
                "ToolGroup classes must have a public constructor that accepts IMcpLogger parameter");
        }

        // Tool 메서드 검증
        ValidateToolMethods(type, report);

        // BaseToolGroup 상속 확인 (선택사항)
        if (type.BaseType?.Name == "BaseToolGroup")
        {
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.ExtendsBaseToolGroup, 
                $"Type '{type.Name}' extends BaseToolGroup (recommended)");
        }
        else if (context.StrictMode)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.NotExtendingBaseToolGroup,
                $"Type '{type.Name}' does not extend BaseToolGroup",
                "Consider extending BaseToolGroup for standard implementations");
        }
    }

    private void ValidateToolMethods(Type type, ValidationReport report)
    {
        var toolMethods = GetToolMethods(type);

        if (toolMethods.Count == 0)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.NoMcpToolMethods, 
                $"Type '{type.Name}' has no methods with [McpTool] attribute");
            return;
        }

        ValidateToolMethodsCollection(toolMethods, type.Name, report);
    }

    private List<MethodInfo> GetToolMethods(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        return methods.Where(m => m.GetCustomAttribute<McpToolAttribute>() != null).ToList();
    }

    private void ValidateToolMethodsCollection(List<MethodInfo> toolMethods, string typeName, ValidationReport report)
    {
        var toolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var method in toolMethods)
        {
            ValidateSingleToolMethod(method, typeName, toolNames, report);
        }
    }

    private void ValidateSingleToolMethod(MethodInfo method, string typeName, HashSet<string> toolNames, ValidationReport report)
    {
        var toolAttr = method.GetCustomAttribute<McpToolAttribute>()!;
        var toolName = toolAttr.Name ?? method.Name;

        ValidateToolNameUniqueness(toolName, typeName, toolNames, report);
        ValidateToolMethodReturnType(method, report);
        ValidateToolMethodParameters(method, report);

        report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.ToolMethodFound, 
            $"Found tool method: '{toolName}' ({method.Name})");
    }

    private void ValidateToolNameUniqueness(string toolName, string typeName, HashSet<string> toolNames, ValidationReport report)
    {
        if (!toolNames.Add(toolName))
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.DuplicateToolName, 
                $"Duplicate tool name '{toolName}' in type '{typeName}'");
        }
    }

    private void ValidateToolMethodReturnType(MethodInfo method, ValidationReport report)
    {
        var returnType = method.ReturnType;
        var isValidReturnType = IsValidReturnType(returnType);

        if (!isValidReturnType)
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.InvalidReturnType,
                $"Tool method '{method.Name}' has invalid return type",
                "Tool methods must return Task or Task<T>");
        }
    }

    private static bool IsValidReturnType(Type returnType)
    {
        if (returnType == typeof(Task))
        {
            return true;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return true;
        }

        return false;
    }

    private void ValidateToolMethodParameters(MethodInfo method, ValidationReport report)
    {
        var parameters = method.GetParameters();
        
        if (parameters.Length > ValidationConstants.ValidationLimits.MaxToolMethodParameters)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.TooManyParameters,
                $"Tool method '{method.Name}' has {parameters.Length} parameters",
                "Tool methods typically accept a single parameter object");
        }
    }

    private void ValidateDependencies(Assembly assembly, ValidationReport report)
    {
        try
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            
            ValidateRequiredDependencies(referencedAssemblies, report);
            ReportExternalDependencies(referencedAssemblies, report);
        }
        catch (Exception ex)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.DependencyAnalysisFailed,
                $"Could not analyze dependencies: {ex.Message}");
        }
    }

    private void ValidateRequiredDependencies(AssemblyName[] referencedAssemblies, ValidationReport report)
    {
        var hasMcpSdk = referencedAssemblies.Any(a => a.Name == ValidationConstants.AssemblyNames.MicubeMcpSdk);
        
        if (!hasMcpSdk)
        {
            report.AddError("DLL", ValidationConstants.ErrorCodes.Dll.MissingSdkReference,
                $"Missing reference to {ValidationConstants.AssemblyNames.MicubeMcpSdk}",
                $"The assembly must reference {ValidationConstants.AssemblyNames.MicubeMcpSdk}");
        }
    }

    private void ReportExternalDependencies(AssemblyName[] referencedAssemblies, ValidationReport report)
    {
        var externalDeps = GetExternalDependencies(referencedAssemblies);

        if (externalDeps.Count > 0)
        {
            var depList = string.Join(", ", externalDeps.Select(a => $"{a.Name} v{a.Version}"));
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.ExternalDependencies, 
                $"External dependencies: {depList}");
        }
    }

    private List<AssemblyName> GetExternalDependencies(AssemblyName[] referencedAssemblies)
    {
        return referencedAssemblies
            .Where(a => !IsSystemOrMicrosoftAssembly(a))
            .Where(a => a.Name != ValidationConstants.AssemblyNames.MicubeMcpSdk)
            .ToList();
    }

    private static bool IsSystemOrMicrosoftAssembly(AssemblyName assemblyName)
    {
        return assemblyName.Name?.StartsWith(ValidationConstants.AssemblyNames.SystemPrefix) == true ||
               assemblyName.Name?.StartsWith(ValidationConstants.AssemblyNames.MicrosoftPrefix) == true;
    }

    private void ValidateSdkCompatibility(Assembly assembly, ValidationReport report)
    {
        try
        {
            var sdkReference = GetSdkReference(assembly);
            if (sdkReference == null) return;

            var currentSdkVersion = GetCurrentSdkVersion();
            var referencedSdkVersion = sdkReference.Version;

            if (currentSdkVersion != null && referencedSdkVersion != null)
            {
                CheckVersionCompatibility(currentSdkVersion, referencedSdkVersion, report);
            }
        }
        catch (Exception ex)
        {
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Dll.SdkCompatibilityCheck,
                $"Could not verify SDK compatibility: {ex.Message}");
        }
    }

    private AssemblyName? GetSdkReference(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .FirstOrDefault(a => a.Name == ValidationConstants.AssemblyNames.MicubeMcpSdk);
    }

    private Version? GetCurrentSdkVersion()
    {
        return typeof(IMcpToolGroup).Assembly.GetName().Version;
    }

    private void CheckVersionCompatibility(Version currentVersion, Version referencedVersion, ValidationReport report)
    {
        if (referencedVersion.Major != currentVersion.Major)
        {
            report.AddWarning("DLL", ValidationConstants.ErrorCodes.Dll.SdkVersionMismatch,
                $"SDK version mismatch - DLL uses v{referencedVersion}, current is v{currentVersion}",
                "Major version differences may cause compatibility issues");
        }
        else if (referencedVersion < currentVersion)
        {
            report.AddInfo("DLL", ValidationConstants.ErrorCodes.Info.SdkVersionInfo,
                $"DLL uses older SDK version (v{referencedVersion}), current is v{currentVersion}");
        }
    }
}