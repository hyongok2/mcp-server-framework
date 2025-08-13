using System.Reflection;
using Micube.MCP.Core.MetaData;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Validator.Models;

namespace Micube.MCP.Validator.Validators;

public class IntegrityValidator : IValidator
{
    public string Name => "Integrity Validator";

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var report = new ValidationReport { Context = context };
        var startTime = DateTime.UtcNow;

        // 사전 조건 확인
        if (string.IsNullOrEmpty(context.DllPath) || string.IsNullOrEmpty(context.ManifestPath))
        {
            report.AddError("Integrity", "INT001", 
                "Both DLL and Manifest paths are required for integrity validation");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        if (!File.Exists(context.DllPath) || !File.Exists(context.ManifestPath))
        {
            report.AddError("Integrity", "INT002", 
                "DLL or Manifest file not found");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        try
        {
            // Manifest 메타데이터 가져오기
            ToolGroupMetadata? manifestMetadata = null;
            if (context.Metadata.ContainsKey("ToolGroupMetadata"))
            {
                manifestMetadata = context.Metadata["ToolGroupMetadata"] as ToolGroupMetadata;
            }

            if (manifestMetadata == null)
            {
                // Manifest를 직접 로드
                var manifestValidator = new ManifestValidator();
                var manifestReport = await manifestValidator.ValidateAsync(context);
                
                if (!manifestReport.IsValid)
                {
                    report.AddError("Integrity", "INT003", 
                        "Failed to load manifest for integrity check",
                        $"{manifestReport.Issues.Count} manifest errors found");
                    report.Duration = DateTime.UtcNow - startTime;
                    return report;
                }

                manifestMetadata = manifestReport.Context.Metadata["ToolGroupMetadata"] as ToolGroupMetadata;
            }

            if (manifestMetadata == null)
            {
                report.AddError("Integrity", "INT004", "Could not retrieve manifest metadata");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // DLL 어셈블리 로드
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(context.DllPath);
            }
            catch (Exception ex)
            {
                report.AddError("Integrity", "INT005", 
                    $"Failed to load DLL for integrity check: {ex.Message}");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // ToolGroup 타입 찾기
            var toolGroupTypes = assembly.GetTypes()
                .Where(t => typeof(IMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            if (toolGroupTypes.Count == 0)
            {
                report.AddError("Integrity", "INT010", 
                    "No IMcpToolGroup implementation found in DLL");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // 각 ToolGroup에 대해 일치성 검증
            bool foundMatchingGroup = false;
            foreach (var type in toolGroupTypes)
            {
                var attr = type.GetCustomAttribute<McpToolGroupAttribute>();
                if (attr == null) continue;

                if (attr.GroupName == manifestMetadata.GroupName)
                {
                    foundMatchingGroup = true;
                    report.AddInfo("Integrity", "INT100", 
                        $"Found matching tool group: '{attr.GroupName}'");

                    // ManifestPath 일치성 검증
                    ValidateManifestPath(attr, context, report);

                    // Tool 메서드와 Manifest Tools 일치성 검증
                    ValidateToolsIntegrity(type, manifestMetadata, report, context.StrictMode);

                    // 버전 정보 비교 (있는 경우)
                    ValidateVersionConsistency(manifestMetadata, assembly, report);
                }
            }

            if (!foundMatchingGroup)
            {
                report.AddError("Integrity", "INT011", 
                    $"No tool group in DLL matches manifest GroupName '{manifestMetadata.GroupName}'",
                    "Ensure the GroupName in McpToolGroup attribute matches the manifest");
            }

        }
        catch (Exception ex)
        {
            report.AddError("Integrity", "INT999", 
                $"Unexpected error during integrity validation: {ex.Message}");
        }

        report.Duration = DateTime.UtcNow - startTime;
        return report;
    }

    private void ValidateManifestPath(McpToolGroupAttribute attr, ValidationContext context, ValidationReport report)
    {
        var expectedManifestName = Path.GetFileName(attr.ManifestPath);
        var actualManifestName = Path.GetFileName(context.ManifestPath);

        if (!string.Equals(expectedManifestName, actualManifestName, StringComparison.OrdinalIgnoreCase))
        {
            report.AddWarning("Integrity", "INT020", 
                $"Manifest filename mismatch - Expected: '{expectedManifestName}', Actual: '{actualManifestName}'",
                "The McpToolGroup attribute references a different manifest filename");
        }
    }

    private void ValidateToolsIntegrity(Type toolGroupType, ToolGroupMetadata manifestMetadata, 
        ValidationReport report, bool strictMode)
    {
        // DLL의 Tool 메서드 수집
        var methods = toolGroupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var toolMethods = methods
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
            .ToDictionary(
                m => m.GetCustomAttribute<McpToolAttribute>()?.Name ?? m.Name,
                m => m,
                StringComparer.OrdinalIgnoreCase
            );

        // Manifest의 Tool 정의 수집
        var manifestTools = manifestMetadata.Tools?
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase) 
            ?? new Dictionary<string, ToolDescriptor>();

        // 1. Manifest에는 있지만 DLL에는 없는 Tool 검사
        foreach (var manifestTool in manifestTools.Keys)
        {
            if (!toolMethods.ContainsKey(manifestTool))
            {
                report.AddError("Integrity", "INT030", 
                    $"Tool '{manifestTool}' defined in manifest but not found in DLL",
                    "Ensure all tools in manifest have corresponding [McpTool] methods in the DLL");
            }
        }

        // 2. DLL에는 있지만 Manifest에는 없는 Tool 검사
        foreach (var dllTool in toolMethods.Keys)
        {
            if (!manifestTools.ContainsKey(dllTool))
            {
                var severity = strictMode ? IssueSeverity.Error : IssueSeverity.Warning;
                var issue = new ValidationIssue
                {
                    Severity = severity,
                    Category = "Integrity",
                    Code = "INT031",
                    Message = $"Tool '{dllTool}' found in DLL but not defined in manifest",
                    Details = "All [McpTool] methods should be documented in the manifest",
                    Suggestion = "Add the tool definition to the manifest file"
                };
                report.AddIssue(issue);
            }
        }

        // 3. 매칭된 Tool의 파라미터 일치성 검증
        foreach (var toolName in manifestTools.Keys.Intersect(toolMethods.Keys))
        {
            var manifestTool = manifestTools[toolName];
            var method = toolMethods[toolName];

            ValidateToolParameters(toolName, manifestTool, method, report, strictMode);
        }

        // 통계 정보
        report.AddInfo("Integrity", "INT101", 
            $"Tool count - Manifest: {manifestTools.Count}, DLL: {toolMethods.Count}, Matched: {manifestTools.Keys.Intersect(toolMethods.Keys).Count()}");
    }

    private void ValidateToolParameters(string toolName, ToolDescriptor manifestTool, 
        MethodInfo method, ValidationReport report, bool strictMode)
    {
        var methodParams = method.GetParameters();
        
        // 메서드가 파라미터를 받는 경우
        if (methodParams.Length > 0)
        {
            var paramType = methodParams[0].ParameterType;
            
            // MCP 도구는 Dictionary<string, object> 파라미터를 사용해야 함
            var expectedType = typeof(Dictionary<string, object>);
            
            if (paramType == expectedType)
            {
                // Dictionary<string, object> 타입이므로 매니페스트 파라미터는 항상 유효함
                // 런타임에 키로 접근하므로 컴파일 타임 검증은 불필요
                if (manifestTool.Parameters != null && manifestTool.Parameters.Count > 0)
                {
                    report.AddInfo("Integrity", "INT040",
                        $"Tool '{toolName}': Found {manifestTool.Parameters.Count} parameter(s) in manifest - parameters will be accessed via Dictionary keys at runtime");
                }
            }
            else
            {
                // Dictionary<string, object>가 아닌 경우 경고
                report.AddWarning("Integrity", "INT041",
                    $"Tool '{toolName}': Method parameter type is '{paramType.Name}' but MCP tools should use 'Dictionary<string, object>'",
                    "Consider changing the parameter type to Dictionary<string, object> for standard MCP compliance");
                    
                // 기존 Property 기반 검증 로직 (Dictionary가 아닌 경우에만)
                var properties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                if (manifestTool.Parameters != null && manifestTool.Parameters.Count > 0)
                {
                    foreach (var manifestParam in manifestTool.Parameters)
                    {
                        var property = properties.FirstOrDefault(p => 
                            string.Equals(p.Name, manifestParam.Name, StringComparison.OrdinalIgnoreCase));

                        if (property == null)
                        {
                            report.AddWarning("Integrity", "INT042",
                                $"Tool '{toolName}': Parameter '{manifestParam.Name}' defined in manifest but not found in method parameter type",
                                $"Check if the parameter type '{paramType.Name}' has property '{manifestParam.Name}'");
                        }
                        else
                        {
                            // 타입 호환성 검증
                            ValidateParameterTypeCompatibility(toolName, manifestParam, property, report);
                        }
                    }
                }
            }
        }
        else if (manifestTool.Parameters != null && manifestTool.Parameters.Count > 0)
        {
            // 메서드는 파라미터가 없는데 Manifest에는 정의된 경우
            report.AddError("Integrity", "INT043",
                $"Tool '{toolName}': Manifest defines {manifestTool.Parameters.Count} parameters but method accepts none");
        }
    }

    private void ValidateParameterTypeCompatibility(string toolName, ParameterDescriptor manifestParam, 
        PropertyInfo property, ValidationReport report)
    {
        var propertyType = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        bool isCompatible = manifestParam.Type.ToLowerInvariant() switch
        {
            "string" => underlyingType == typeof(string),
            "int" or "integer" => underlyingType == typeof(int) || underlyingType == typeof(long),
            "number" => underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal),
            "bool" or "boolean" => underlyingType == typeof(bool),
            "object" => underlyingType == typeof(object) || underlyingType.IsClass,
            "array" => underlyingType.IsArray || underlyingType.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)),
            _ => false
        };

        if (!isCompatible)
        {
            report.AddWarning("Integrity", "INT043",
                $"Tool '{toolName}': Parameter '{manifestParam.Name}' type mismatch - Manifest: '{manifestParam.Type}', Code: '{underlyingType.Name}'");
        }
    }

    private bool IsNullableType(Type type)
    {
        if (!type.IsValueType) return true; // Reference types are nullable
        return Nullable.GetUnderlyingType(type) != null;
    }

    private void ValidateVersionConsistency(ToolGroupMetadata manifestMetadata, Assembly assembly, ValidationReport report)
    {
        var assemblyVersion = assembly.GetName().Version?.ToString();
        
        if (!string.IsNullOrEmpty(manifestMetadata.Version) && !string.IsNullOrEmpty(assemblyVersion))
        {
            // 버전 형식 정규화 (Major.Minor.Build 까지만 비교)
            var normalizedManifestVersion = NormalizeVersion(manifestMetadata.Version);
            var normalizedAssemblyVersion = NormalizeVersion(assemblyVersion);

            if (normalizedManifestVersion != normalizedAssemblyVersion)
            {
                report.AddInfo("Integrity", "INT050",
                    $"Version mismatch - Manifest: '{manifestMetadata.Version}', Assembly: '{assemblyVersion}'",
                    "Consider keeping manifest and assembly versions synchronized");
            }
        }
    }

    private string NormalizeVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length >= 3)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        }
        return version;
    }
}