using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Services;
using Micube.MCP.Validator.Constants;

namespace Micube.MCP.Validator.Utilities;

public static class ValidationHelper
{
    public static ValidationReport CreateErrorReport(ValidationContext context, string category, string errorCode, string message, string? details = null)
    {
        var report = new ValidationReport { Context = context };
        report.AddError(category, errorCode, message, details);
        return report;
    }

    public static void SetReportDuration(ValidationReport report, DateTime startTime)
    {
        report.Duration = DateTime.UtcNow - startTime;
    }

    public static ValidationReport HandleException<T>(
        Exception ex, 
        ValidationContext context, 
        DateTime startTime, 
        FileLogger? logger, 
        string operationName, 
        string errorCode) where T : class
    {
        var duration = DateTime.UtcNow - startTime;
        var typeName = typeof(T).Name;
        
        logger?.LogCritical(typeName, $"Unexpected error during {operationName}", 
            $"Duration: {duration.TotalMilliseconds:F0}ms", ex);
        
        var errorReport = new ValidationReport { Context = context };
        errorReport.AddError(typeName, errorCode, $"Unexpected error during {operationName}: {ex.Message}", ex.ToString());
        errorReport.Duration = duration;
        
        return errorReport;
    }

    public static bool ValidateFileExists(string? filePath, ValidationReport report, string category, DateTime startTime)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            report.AddError(category, GetPathNotSpecifiedError(category), $"{category} path is not specified");
            SetReportDuration(report, startTime);
            return false;
        }

        if (!File.Exists(filePath))
        {
            report.AddError(category, GetFileNotFoundError(category), $"{category} file not found: {filePath}");
            SetReportDuration(report, startTime);
            return false;
        }

        return true;
    }

    private static string GetPathNotSpecifiedError(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "dll" => ValidationConstants.ErrorCodes.Dll.PathNotSpecified,
            "manifest" => ValidationConstants.ErrorCodes.Manifest.PathNotSpecified,
            _ => "PATH001"
        };
    }

    private static string GetFileNotFoundError(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "dll" => ValidationConstants.ErrorCodes.Dll.FileNotFound,
            "manifest" => ValidationConstants.ErrorCodes.Manifest.FileNotFound,
            _ => "FILE001"
        };
    }

    public static void LogValidationStart(FileLogger? logger, string validatorName, string? targetPath)
    {
        logger?.LogValidationStart(validatorName, targetPath);
    }

    public static void LogValidationEnd(FileLogger? logger, string validatorName, string? targetPath, TimeSpan duration)
    {
        logger?.LogValidationEnd(validatorName, targetPath, duration);
    }

    public static void LogValidationResult(FileLogger? logger, string validatorName, ValidationReport report)
    {
        logger?.LogInfo(validatorName, "Validation completed", 
            $"Issues: {report.Issues.Count}, Errors: {report.Issues.Count(i => i.Severity == IssueSeverity.Error)}");
    }

    public static bool IsValidParameterType(string type)
    {
        var validTypes = new[] { "string", "int", "integer", "number", "bool", "boolean", "object", "array" };
        return validTypes.Contains(type.ToLowerInvariant());
    }

    public static string[] GetValidParameterTypes()
    {
        return new[] { "string", "int", "integer", "number", "bool", "boolean", "object", "array" };
    }
}