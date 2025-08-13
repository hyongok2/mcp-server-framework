using System.Text.Json;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Validator.Models;

namespace Micube.MCP.Validator.Validators;

public class ManifestValidator : IValidator
{
    public string Name => "Manifest Validator";

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var report = new ValidationReport { Context = context };
        var startTime = DateTime.UtcNow;

        if (string.IsNullOrEmpty(context.ManifestPath))
        {
            report.AddError("Manifest", "MAN001", "Manifest path is not specified");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        // 파일 존재 여부 확인
        if (!File.Exists(context.ManifestPath))
        {
            report.AddError("Manifest", "MAN002", $"Manifest file not found: {context.ManifestPath}");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        report.Statistics.ValidatedFiles.Add(context.ManifestPath);

        try
        {
            var json = await File.ReadAllTextAsync(context.ManifestPath);
            
            // 빈 파일 검증
            if (string.IsNullOrWhiteSpace(json))
            {
                report.AddError("Manifest", "MAN003", "Manifest file is empty", context.ManifestPath);
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // JSON 파싱
            ToolGroupMetadata? metadata = null;
            try
            {
                metadata = JsonSerializer.Deserialize<ToolGroupMetadata>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });
            }
            catch (JsonException ex)
            {
                report.AddError("Manifest", "MAN004", "Invalid JSON format", 
                    $"Error at line {ex.LineNumber}: {ex.Message}");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            if (metadata == null)
            {
                report.AddError("Manifest", "MAN005", "Failed to deserialize manifest");
                report.Duration = DateTime.UtcNow - startTime;
                return report;
            }

            // 메타데이터 저장
            context.Metadata["ToolGroupMetadata"] = metadata;

            // 필수 필드 검증
            ValidateRequiredFields(metadata, report);

            // Tool 배열 검증
            ValidateTools(metadata, report, context.StrictMode);

            // 버전 형식 검증
            ValidateVersion(metadata, report);

            report.AddInfo("Manifest", "MAN100", 
                $"Successfully validated manifest with {metadata.Tools?.Count ?? 0} tools");
        }
        catch (Exception ex)
        {
            report.AddError("Manifest", "MAN999", $"Unexpected error during validation: {ex.Message}");
        }

        report.Duration = DateTime.UtcNow - startTime;
        return report;
    }

    private void ValidateRequiredFields(ToolGroupMetadata metadata, ValidationReport report)
    {
        // GroupName 검증
        if (string.IsNullOrWhiteSpace(metadata.GroupName))
        {
            report.AddError("Manifest", "MAN010", "Missing or empty 'GroupName' field");
        }
        else if (metadata.GroupName.Length > 100)
        {
            report.AddWarning("Manifest", "MAN011", $"GroupName '{metadata.GroupName}' is unusually long (>100 chars)");
        }

        // Description 검증
        if (string.IsNullOrWhiteSpace(metadata.Description))
        {
            report.AddWarning("Manifest", "MAN012", "Missing or empty 'Description' field");
        }

        // Tools 배열 검증
        if (metadata.Tools == null)
        {
            report.AddError("Manifest", "MAN013", "Missing 'Tools' array");
        }
        else if (metadata.Tools.Count == 0)
        {
            report.AddWarning("Manifest", "MAN014", "Tools array is empty");
        }
    }

    private void ValidateTools(ToolGroupMetadata metadata, ValidationReport report, bool strictMode)
    {
        if (metadata.Tools == null) return;

        var toolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < metadata.Tools.Count; i++)
        {
            var tool = metadata.Tools[i];
            var toolIdentifier = $"Tool[{i}]";

            // Tool 이름 검증
            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                report.AddError("Manifest", "MAN020", 
                    $"{toolIdentifier}: Missing or empty 'Name' field");
            }
            else
            {
                toolIdentifier = $"Tool '{tool.Name}'";

                // 중복 검사
                if (!toolNames.Add(tool.Name))
                {
                    report.AddError("Manifest", "MAN021", 
                        $"{toolIdentifier}: Duplicate tool name found");
                }

                // 이름 규칙 검증
                if (!System.Text.RegularExpressions.Regex.IsMatch(tool.Name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                {
                    var severity = strictMode ? IssueSeverity.Error : IssueSeverity.Warning;
                    var issue = new ValidationIssue
                    {
                        Severity = severity,
                        Category = "Manifest",
                        Code = "MAN022",
                        Message = $"{toolIdentifier}: Tool name should start with a letter and contain only letters, numbers, and underscores",
                        Suggestion = "Consider using PascalCase or camelCase naming convention"
                    };
                    report.AddIssue(issue);
                }
            }

            // Description 검증
            if (string.IsNullOrWhiteSpace(tool.Description))
            {
                report.AddWarning("Manifest", "MAN023", 
                    $"{toolIdentifier}: Missing or empty 'Description' field");
            }

            // Parameters 검증
            ValidateParameters(tool, toolIdentifier, report, strictMode);

            // StructuredOutput 검증
            if (tool.StructuredOutput && tool.OutputSchema == null)
            {
                report.AddWarning("Manifest", "MAN024",
                    $"{toolIdentifier}: StructuredOutput is true but OutputSchema is not defined");
            }
        }
    }

    private void ValidateParameters(ToolDescriptor tool, string toolIdentifier, ValidationReport report, bool strictMode)
    {
        if (tool.Parameters == null || tool.Parameters.Count == 0)
        {
            if (strictMode)
            {
                report.AddInfo("Manifest", "MAN030", 
                    $"{toolIdentifier}: No parameters defined");
            }
            return;
        }

        var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasOptional = false;

        foreach (var param in tool.Parameters)
        {
            var paramIdentifier = $"{toolIdentifier}.{param.Name ?? "unnamed"}";

            // 파라미터 이름 검증
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                report.AddError("Manifest", "MAN031", 
                    $"{toolIdentifier}: Parameter with empty name found");
                continue;
            }

            // 중복 검사
            if (!paramNames.Add(param.Name))
            {
                report.AddError("Manifest", "MAN032", 
                    $"{paramIdentifier}: Duplicate parameter name");
            }

            // 타입 검증
            var validTypes = new[] { "string", "int", "integer", "number", "bool", "boolean", "object", "array" };
            if (!validTypes.Contains(param.Type.ToLowerInvariant()))
            {
                report.AddError("Manifest", "MAN033", 
                    $"{paramIdentifier}: Invalid type '{param.Type}'",
                    $"Valid types: {string.Join(", ", validTypes)}");
            }

            // Required/Optional 순서 검증
            if (param.Required)
            {
                if (hasOptional)
                {
                    report.AddWarning("Manifest", "MAN034",
                        $"{paramIdentifier}: Required parameter defined after optional parameters",
                        "Consider placing all required parameters before optional ones");
                }
            }
            else
            {
                hasOptional = true;
            }

            // Description 검증
            if (string.IsNullOrWhiteSpace(param.Description) && strictMode)
            {
                report.AddWarning("Manifest", "MAN035",
                    $"{paramIdentifier}: Missing parameter description");
            }
        }
    }

    private void ValidateVersion(ToolGroupMetadata metadata, ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(metadata.Version))
        {
            report.AddInfo("Manifest", "MAN040", "Version not specified, defaulting to 1.0.0");
            return;
        }

        // Semantic versioning 검증 (간단한 패턴)
        var versionPattern = @"^\d+\.\d+(\.\d+)?(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(metadata.Version, versionPattern))
        {
            report.AddWarning("Manifest", "MAN041",
                $"Version '{metadata.Version}' does not follow semantic versioning",
                "Expected format: MAJOR.MINOR.PATCH (e.g., 1.0.0)");
        }
    }
}