namespace Micube.MCP.Validator.Models;

public class ValidationReport
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ValidatorVersion { get; set; } = "1.0.0";
    public ValidationContext Context { get; set; } = new();
    public List<ValidationIssue> Issues { get; set; } = new();
    public ValidationStatistics Statistics { get; set; } = new();
    public bool IsValid => !Issues.Any(i => i.Severity == IssueSeverity.Error);
    public TimeSpan Duration { get; set; }

    public void AddIssue(ValidationIssue issue)
    {
        Issues.Add(issue);
        UpdateStatistics();
    }

    public void AddError(string category, string code, string message, string? details = null)
    {
        AddIssue(new ValidationIssue
        {
            Severity = IssueSeverity.Error,
            Category = category,
            Code = code,
            Message = message,
            Details = details
        });
    }

    public void AddWarning(string category, string code, string message, string? details = null)
    {
        AddIssue(new ValidationIssue
        {
            Severity = IssueSeverity.Warning,
            Category = category,
            Code = code,
            Message = message,
            Details = details
        });
    }

    public void AddInfo(string category, string code, string message, string? details = null)
    {
        AddIssue(new ValidationIssue
        {
            Severity = IssueSeverity.Info,
            Category = category,
            Code = code,
            Message = message,
            Details = details
        });
    }

    private void UpdateStatistics()
    {
        Statistics.TotalIssues = Issues.Count;
        Statistics.ErrorCount = Issues.Count(i => i.Severity == IssueSeverity.Error);
        Statistics.WarningCount = Issues.Count(i => i.Severity == IssueSeverity.Warning);
        Statistics.InfoCount = Issues.Count(i => i.Severity == IssueSeverity.Info);
    }
}

public class ValidationStatistics
{
    public int TotalIssues { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public List<string> ValidatedFiles { get; set; } = new();
    public Dictionary<string, int> IssuesByCategory { get; set; } = new();
}