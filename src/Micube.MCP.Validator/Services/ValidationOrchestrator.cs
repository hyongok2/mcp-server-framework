using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Validators;
using Micube.MCP.Validator.Constants;
using System.Reflection;
using Micube.MCP.SDK.Attributes;

namespace Micube.MCP.Validator.Services;

public class ValidationOrchestrator
{
    private readonly List<IValidator> _validators;
    private readonly FileLogger? _logger;

    public ValidationOrchestrator(FileLogger? logger = null)
    {
        _logger = logger;
        _validators = new List<IValidator>
        {
            new ManifestValidator(),
            new DllValidator(_logger),
            new IntegrityValidator(),
            new RuntimeValidator()
        };

        _logger?.LogInfo("ValidationOrchestrator", "Orchestrator initialized", 
            $"Validators: {string.Join(", ", _validators.Select(v => v.Name))}");
    }

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger?.LogValidationStart("ValidationOrchestrator", context.DirectoryPath ?? context.DllPath);
        
        try
        {
            var orchestratorReport = new ValidationReport { Context = context };

            // 디렉토리 검증 모드
            if (!string.IsNullOrEmpty(context.DirectoryPath))
            {
                _logger?.LogInfo("ValidationOrchestrator", "Starting directory validation", context.DirectoryPath);
                orchestratorReport = await ValidateDirectoryAsync(context);
            }
            // 단일 파일 검증 모드
            else
            {
                _logger?.LogInfo("ValidationOrchestrator", "Starting single file validation", 
                    $"DLL: {context.DllPath}, Manifest: {context.ManifestPath}");
                orchestratorReport = await ValidateSingleAsync(context);
            }

            orchestratorReport.Duration = DateTime.UtcNow - startTime;
            UpdateStatistics(orchestratorReport);

            _logger?.LogValidationEnd("ValidationOrchestrator", context.DirectoryPath ?? context.DllPath, orchestratorReport.Duration);
            _logger?.LogInfo("ValidationOrchestrator", "Validation completed", 
                $"IsValid: {orchestratorReport.IsValid}, Issues: {orchestratorReport.Issues.Count}, " +
                $"Errors: {orchestratorReport.Issues.Count(i => i.Severity == IssueSeverity.Error)}");

            return orchestratorReport;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogCritical("ValidationOrchestrator", "Validation process crashed", 
                $"Duration before crash: {duration.TotalMilliseconds:F0}ms", ex);
            
            // 예외가 발생해도 기본 리포트 반환
            var errorReport = new ValidationReport { Context = context };
            errorReport.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.ProcessingCrashed, 
                $"Validation process crashed: {ex.Message}", ex.ToString());
            errorReport.Duration = duration;
            
            return errorReport;
        }
    }

    private async Task<ValidationReport> ValidateSingleAsync(ValidationContext context)
    {
        try
        {
            var combinedReport = new ValidationReport { Context = context };

            // 검증 레벨에 따른 검증기 선택
            var selectedValidators = SelectValidators(context);
            
            _logger?.LogInfo("ValidateSingleAsync", "Selected validators", 
                $"Count: {selectedValidators.Count}, Validators: {string.Join(", ", selectedValidators.Select(v => v.Name))}");

            foreach (var validator in selectedValidators)
            {
                try
                {
                    _logger?.LogValidationStart(validator.Name, context.DllPath ?? context.ManifestPath);
                    var startTime = DateTime.UtcNow;
                    
                    var report = await validator.ValidateAsync(context);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger?.LogValidationEnd(validator.Name, context.DllPath ?? context.ManifestPath, duration);
                    _logger?.LogInfo("ValidateSingleAsync", $"Validator {validator.Name} completed", 
                        $"Issues: {report.Issues.Count}, Errors: {report.Issues.Count(i => i.Severity == IssueSeverity.Error)}");
                    
                    // 이슈 병합
                    foreach (var issue in report.Issues)
                    {
                        combinedReport.AddIssue(issue);
                    }

                    // 검증된 파일 목록 병합
                    foreach (var file in report.Statistics.ValidatedFiles)
                    {
                        if (!combinedReport.Statistics.ValidatedFiles.Contains(file))
                        {
                            combinedReport.Statistics.ValidatedFiles.Add(file);
                        }
                    }

                    // 메타데이터 병합
                    foreach (var kvp in report.Context.Metadata)
                    {
                        if (!context.Metadata.ContainsKey(kvp.Key))
                        {
                            context.Metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("ValidateSingleAsync", $"Validator {validator.Name} failed", 
                        "Exception during validation", ex);
                    
                    combinedReport.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.ValidatorFailed, 
                        $"Validator '{validator.Name}' failed: {ex.Message}", ex.ToString());
                }
            }

            _logger?.LogInfo("ValidateSingleAsync", "Single validation completed", 
                $"Total issues: {combinedReport.Issues.Count}");

            return combinedReport;
        }
        catch (Exception ex)
        {
            _logger?.LogCritical("ValidateSingleAsync", "Critical error in single validation", 
                "Unexpected exception", ex);
            
            var errorReport = new ValidationReport { Context = context };
            errorReport.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.SingleValidationCrashed, 
                $"Single validation crashed: {ex.Message}", ex.ToString());
            return errorReport;
        }
    }

    private async Task<ValidationReport> ValidateDirectoryAsync(ValidationContext context)
    {
        try
        {
            var directoryReport = new ValidationReport { Context = context };
            _logger?.LogInfo("ValidateDirectoryAsync", "Starting directory validation", context.DirectoryPath);

            if (!ValidateDirectoryExists(context.DirectoryPath!, directoryReport))
            {
                return directoryReport;
            }

            var dllFiles = FindDllFiles(context.DirectoryPath!, directoryReport);
            if (dllFiles == null)
            {
                return directoryReport;
            }

            var validatedDlls = await ProcessDllFiles(dllFiles, context, directoryReport);

            AddDirectoryValidationSummary(directoryReport, dllFiles.Length, validatedDlls);

            return directoryReport;
        }
        catch (Exception ex)
        {
            return HandleDirectoryValidationError(ex, context);
        }
    }

    private bool ValidateDirectoryExists(string directoryPath, ValidationReport report)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger?.LogError("ValidateDirectoryAsync", "Directory not found", directoryPath);
            report.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.DirectoryNotFound, 
                $"Directory not found: {directoryPath}");
            return false;
        }
        return true;
    }

    private string[]? FindDllFiles(string directoryPath, ValidationReport report)
    {
        try
        {
            _logger?.LogInfo("ValidateDirectoryAsync", "Searching for DLL files", directoryPath);
            var dllFiles = Directory.GetFiles(directoryPath, ValidationConstants.FilePatterns.DllExtension, SearchOption.AllDirectories)
                .Where(f => !f.Contains(ValidationConstants.FilePatterns.ObjDirectory) && 
                           !f.Contains(ValidationConstants.FilePatterns.ObjDirectoryUnix))
                .ToArray();
            
            _logger?.LogInfo("ValidateDirectoryAsync", "DLL files found", 
                $"Count: {dllFiles.Length}, Files: [{string.Join(", ", dllFiles.Select(Path.GetFileName))}]");
            
            if (dllFiles.Length == 0)
            {
                _logger?.LogWarning("ValidateDirectoryAsync", "No DLL files found", directoryPath);
                report.AddWarning("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.NoDllFilesFound, 
                    $"No DLL files found in directory: {directoryPath}");
                return null;
            }

            report.AddInfo("Orchestrator", ValidationConstants.ErrorCodes.Info.OrchestratorInfo, 
                $"Found {dllFiles.Length} DLL file(s) to validate");

            return dllFiles;
        }
        catch (Exception ex)
        {
            _logger?.LogError("ValidateDirectoryAsync", "Error during DLL processing", 
                "Exception while processing DLL files", ex);
            
            report.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.DirectoryProcessingError, 
                $"Error processing directory: {ex.Message}", ex.ToString());
            return null;
        }
    }

    private async Task<int> ProcessDllFiles(string[] dllFiles, ValidationContext context, ValidationReport directoryReport)
    {
        var validatedDlls = 0;
        
        foreach (var dllFile in dllFiles)
        {
            try
            {
                if (await ProcessSingleDllFile(dllFile, context, directoryReport))
                {
                    validatedDlls++;
                }
            }
            catch (Exception ex)
            {
                HandleDllProcessingError(dllFile, ex, directoryReport);
            }
        }
        
        return validatedDlls;
    }

    private async Task<bool> ProcessSingleDllFile(string dllFile, ValidationContext context, ValidationReport directoryReport)
    {
        _logger?.LogInfo("ValidateDirectoryAsync", "Processing DLL", $"File: {Path.GetFileName(dllFile)}");
        
        var dllName = Path.GetFileName(dllFile);
        
        if (await ShouldSkipDll(dllFile, directoryReport))
        {
            return false;
        }

        var expectedManifestFileName = GetManifestFileNameFromDll(dllFile);
        if (string.IsNullOrEmpty(expectedManifestFileName))
        {
            LogMissingManifestAttribute(dllName, directoryReport);
            return false;
        }

        var manifestFile = Path.Combine(Path.GetDirectoryName(dllFile)!, expectedManifestFileName);
        if (!File.Exists(manifestFile))
        {
            LogMissingManifestFile(dllName, expectedManifestFileName, directoryReport);
            return false;
        }

        await ValidateDllManifestPair(dllFile, manifestFile, context, directoryReport);
        return true;
    }

    private async Task<bool> ShouldSkipDll(string dllFile, ValidationReport directoryReport)
    {
        var dllName = Path.GetFileName(dllFile);
        
        if (dllName.Equals(ValidationConstants.AssemblyNames.MicubeMcpSdkDll, StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogInfo("ValidateDirectoryAsync", "Skipped SDK DLL", dllName);
            directoryReport.AddInfo("Orchestrator", ValidationConstants.ErrorCodes.Info.SkippedDependencyDll, 
                $"Skipped SDK DLL: {dllName}");
            return true;
        }

        var quickCheck = await ValidateSingleAsync(new ValidationContext
        {
            DllPath = dllFile,
            Level = ValidationLevel.Basic
        });

        var isDependencyDll = quickCheck.Issues.Any(i => 
            (i.Code == ValidationConstants.ErrorCodes.Dll.TypeLoadWarning || 
             i.Code == ValidationConstants.ErrorCodes.Dll.AnalysisWarning || 
             i.Code == ValidationConstants.ErrorCodes.Dll.NoToolGroupImplementation) && 
            i.Severity == IssueSeverity.Warning);

        if (isDependencyDll)
        {
            _logger?.LogInfo("ValidateDirectoryAsync", "Skipped dependency DLL", dllName);
            directoryReport.AddInfo("Orchestrator", ValidationConstants.ErrorCodes.Info.SkippedDependencyDll, 
                $"Skipped dependency DLL: {dllName}");
            return true;
        }

        return false;
    }

    private void LogMissingManifestAttribute(string dllName, ValidationReport report)
    {
        _logger?.LogWarning("ValidateDirectoryAsync", "No McpToolGroup attribute found", dllName);
        report.AddWarning("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.NoMcpToolGroupAttribute, 
            $"No McpToolGroup attribute found or manifest filename not specified in DLL: {dllName}");
    }

    private void LogMissingManifestFile(string dllName, string expectedManifestFileName, ValidationReport report)
    {
        _logger?.LogWarning("ValidateDirectoryAsync", "Expected manifest file not found", 
            $"DLL: {dllName}, Expected: {expectedManifestFileName}");
        report.AddWarning("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.ManifestFileNotFound, 
            $"Expected manifest file not found: {expectedManifestFileName} for DLL: {dllName}");
    }

    private async Task ValidateDllManifestPair(string dllFile, string manifestFile, ValidationContext context, ValidationReport directoryReport)
    {
        var fileContext = new ValidationContext
        {
            DllPath = dllFile,
            ManifestPath = manifestFile,
            StrictMode = context.StrictMode,
            Level = context.Level
        };

        _logger?.LogInfo("ValidateDirectoryAsync", "Starting MCP tool validation", 
            $"DLL: {Path.GetFileName(dllFile)}, Manifest: {Path.GetFileName(manifestFile)}");

        directoryReport.AddInfo("Orchestrator", ValidationConstants.ErrorCodes.Info.OrchestratorInfo, 
            $"Validating MCP tool: {Path.GetFileName(dllFile)} with {Path.GetFileName(manifestFile)}");

        var report = await ValidateSingleAsync(fileContext);

        MergeValidationResults(report, directoryReport, dllFile);

        _logger?.LogInfo("ValidateDirectoryAsync", "MCP tool validation completed", 
            $"DLL: {Path.GetFileName(dllFile)}, Issues: {report.Issues.Count}");
    }

    private void MergeValidationResults(ValidationReport sourceReport, ValidationReport targetReport, string dllFile)
    {
        foreach (var issue in sourceReport.Issues)
        {
            issue.FilePath = issue.FilePath ?? dllFile;
            targetReport.AddIssue(issue);
        }

        foreach (var file in sourceReport.Statistics.ValidatedFiles)
        {
            if (!targetReport.Statistics.ValidatedFiles.Contains(file))
            {
                targetReport.Statistics.ValidatedFiles.Add(file);
            }
        }
    }

    private void HandleDllProcessingError(string dllFile, Exception ex, ValidationReport report)
    {
        var fileName = Path.GetFileName(dllFile);
        _logger?.LogError("ValidateDirectoryAsync", $"Error processing DLL: {fileName}", 
            "Exception during individual DLL processing", ex);
        
        report.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.DllProcessingFailed, 
            $"Failed to process DLL '{fileName}': {ex.Message}", ex.ToString());
    }

    private void AddDirectoryValidationSummary(ValidationReport report, int totalDlls, int validatedDlls)
    {
        report.AddInfo("Orchestrator", ValidationConstants.ErrorCodes.Info.ValidationComplete, 
            $"Validated {validatedDlls} MCP tool DLL(s) out of {totalDlls} total DLL(s)");

        _logger?.LogInfo("ValidateDirectoryAsync", "Directory validation completed", 
            $"Total DLLs: {totalDlls}, Validated: {validatedDlls}, Issues: {report.Issues.Count}");
    }

    private ValidationReport HandleDirectoryValidationError(Exception ex, ValidationContext context)
    {
        _logger?.LogCritical("ValidateDirectoryAsync", "Critical error in directory validation", 
            "Unexpected exception", ex);
        
        var errorReport = new ValidationReport { Context = context };
        errorReport.AddError("Orchestrator", ValidationConstants.ErrorCodes.Orchestrator.DirectoryValidationCrashed, 
            $"Directory validation crashed: {ex.Message}", ex.ToString());
        return errorReport;
    }

    private string? GetManifestFileNameFromDll(string dllPath)
    {
        try
        {
            _logger?.LogInfo("GetManifestFileNameFromDll", "Loading assembly", $"DLL: {Path.GetFileName(dllPath)}");
            
            // DLL 어셈블리 로드
            var assembly = Assembly.LoadFrom(dllPath);
            
            _logger?.LogInfo("GetManifestFileNameFromDll", "Searching for McpToolGroup attribute", 
                $"Assembly: {assembly.GetName().Name}");
            
            // McpToolGroup 어트리뷰트를 가진 타입 찾기
            foreach (var type in assembly.GetTypes())
            {
                var mcpAttribute = type.GetCustomAttribute<McpToolGroupAttribute>();
                if (mcpAttribute != null)
                {
                    _logger?.LogInfo("GetManifestFileNameFromDll", "Found McpToolGroup attribute", 
                        $"Type: {type.Name}, ManifestPath: {mcpAttribute.ManifestPath}");
                    
                    // McpToolGroupAttribute의 ManifestPath 속성이 manifest 파일명
                    return mcpAttribute.ManifestPath;
                }
            }
            
            _logger?.LogWarning("GetManifestFileNameFromDll", "No McpToolGroup attribute found", 
                $"DLL: {Path.GetFileName(dllPath)}");
        }
        catch (Exception ex)
        {
            _logger?.LogError("GetManifestFileNameFromDll", "Failed to extract manifest filename", 
                $"DLL: {Path.GetFileName(dllPath)}", ex);
        }

        return null;
    }

    private List<IValidator> SelectValidators(ValidationContext context)
    {
        var validators = new List<IValidator>();

        // Manifest 검증기는 manifest 경로가 있을 때만
        if (!string.IsNullOrEmpty(context.ManifestPath))
        {
            validators.Add(_validators.First(v => v is ManifestValidator));
        }

        // DLL 검증기는 DLL 경로가 있을 때만
        if (!string.IsNullOrEmpty(context.DllPath))
        {
            validators.Add(_validators.First(v => v is DllValidator));
        }

        // Integrity 검증기는 둘 다 있을 때만
        if (!string.IsNullOrEmpty(context.ManifestPath) && !string.IsNullOrEmpty(context.DllPath))
        {
            validators.Add(_validators.First(v => v is IntegrityValidator));

            // Runtime 검증기는 Full 레벨일 때만
            if (context.Level == ValidationLevel.Full)
            {
                validators.Add(_validators.First(v => v is RuntimeValidator));
            }
        }

        return validators;
    }

    private void UpdateStatistics(ValidationReport report)
    {
        // 카테고리별 이슈 집계
        report.Statistics.IssuesByCategory.Clear();
        
        foreach (var issue in report.Issues)
        {
            if (!report.Statistics.IssuesByCategory.ContainsKey(issue.Category))
            {
                report.Statistics.IssuesByCategory[issue.Category] = 0;
            }
            report.Statistics.IssuesByCategory[issue.Category]++;
        }
    }
}