namespace Micube.MCP.Validator.Models;

public class ValidationIssue
{
    public IssueSeverity Severity { get; set; }
    public string Category { get; set; } = "";
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Details { get; set; }
    public string? FilePath { get; set; }
    public int? Line { get; set; }
    public string? Suggestion { get; set; }
}

public enum IssueSeverity
{
    Error,      // 반드시 수정 필요
    Warning,    // 권장사항
    Info        // 정보성 메시지
}