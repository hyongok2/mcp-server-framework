using System.Text;
using System.Text.Json;
using Micube.MCP.Validator.Models;
using Spectre.Console;

namespace Micube.MCP.Validator.Services;

public class ReportGenerator
{
    public void PrintConsoleReport(ValidationReport report)
    {
        AnsiConsole.Clear();
        
        // 헤더
        var rule = new Rule("[bold yellow]MCP Tool Validation Report[/]")
            .RuleStyle("yellow")
            .LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // 요약 정보
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Property", c => c.Width(20))
            .AddColumn("Value");

        summaryTable.AddRow("Validation Time", report.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        summaryTable.AddRow("Duration", $"{report.Duration.TotalMilliseconds:F0}ms");
        summaryTable.AddRow("Validator Version", report.ValidatorVersion);
        summaryTable.AddRow("Validation Level", report.Context.Level.ToString());
        summaryTable.AddRow("Strict Mode", report.Context.StrictMode ? "Yes" : "No");
        
        if (!string.IsNullOrEmpty(report.Context.DllPath))
            summaryTable.AddRow("DLL", Path.GetFileName(report.Context.DllPath));
        
        if (!string.IsNullOrEmpty(report.Context.ManifestPath))
            summaryTable.AddRow("Manifest", Path.GetFileName(report.Context.ManifestPath));

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        // 검증 결과
        PrintValidationResult(report);
        
        // 통계
        PrintStatistics(report);

        // 이슈 상세
        if (report.Issues.Count > 0)
        {
            PrintIssues(report);
        }

        // 최종 결과
        AnsiConsole.WriteLine();
        if (report.IsValid)
        {
            AnsiConsole.MarkupLine("[bold green]✅ VALIDATION PASSED[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold red]❌ VALIDATION FAILED[/]");
        }
    }

    private void PrintValidationResult(ValidationReport report)
    {
        var resultPanel = new Panel(new BarChart()
            .Width(60)
            .Label("Issue Distribution")
            .AddItem("Errors", report.Statistics.ErrorCount, report.Statistics.ErrorCount > 0 ? Color.Red : Color.Grey)
            .AddItem("Warnings", report.Statistics.WarningCount, report.Statistics.WarningCount > 0 ? Color.Yellow : Color.Grey)
            .AddItem("Info", report.Statistics.InfoCount, Color.Blue))
            .Header("[bold]Validation Results[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(resultPanel);
        AnsiConsole.WriteLine();
    }

    private void PrintStatistics(ValidationReport report)
    {
        if (report.Statistics.IssuesByCategory.Count == 0) return;

        var statsTable = new Table()
            .Border(TableBorder.Simple)
            .Title("[bold]Issues by Category[/]")
            .AddColumn("Category")
            .AddColumn("Count", c => c.RightAligned());

        foreach (var kvp in report.Statistics.IssuesByCategory.OrderByDescending(x => x.Value))
        {
            statsTable.AddRow(kvp.Key, kvp.Value.ToString());
        }

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();
    }

    private void PrintIssues(ValidationReport report)
    {
        var tree = new Tree("[bold]Issues Detail[/]");

        // 심각도별로 그룹화
        var groupedIssues = report.Issues.GroupBy(i => i.Severity);

        foreach (var group in groupedIssues.OrderBy(g => g.Key))
        {
            var severityNode = group.Key switch
            {
                IssueSeverity.Error => tree.AddNode("[red]Errors[/]"),
                IssueSeverity.Warning => tree.AddNode("[yellow]Warnings[/]"),
                IssueSeverity.Info => tree.AddNode("[blue]Information[/]"),
                _ => tree.AddNode("Unknown")
            };

            // 카테고리별로 다시 그룹화
            var categoryGroups = group.GroupBy(i => i.Category);
            
            foreach (var categoryGroup in categoryGroups)
            {
                var categoryNode = severityNode.AddNode($"[bold]{categoryGroup.Key}[/]");

                foreach (var issue in categoryGroup)
                {
                    var issueText = $"[dim]{issue.Code}[/]: {issue.Message}";
                    
                    if (!string.IsNullOrEmpty(issue.Details))
                    {
                        issueText += $"\n  [dim]{issue.Details}[/]";
                    }
                    
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        issueText += $"\n  [italic green]💡 {issue.Suggestion}[/]";
                    }

                    categoryNode.AddNode(issueText);
                }
            }
        }

        AnsiConsole.Write(tree);
    }

    public async Task SaveJsonReportAsync(ValidationReport report, string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(outputPath, json);
    }

    public async Task SaveMarkdownReportAsync(ValidationReport report, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# MCP Tool Validation Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.Timestamp:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"**Duration:** {report.Duration.TotalMilliseconds:F0}ms");
        sb.AppendLine($"**Validator Version:** {report.ValidatorVersion}");
        sb.AppendLine();

        // 검증 대상
        sb.AppendLine("## Validation Target");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(report.Context.DllPath))
            sb.AppendLine($"- **DLL:** `{report.Context.DllPath}`");
        if (!string.IsNullOrEmpty(report.Context.ManifestPath))
            sb.AppendLine($"- **Manifest:** `{report.Context.ManifestPath}`");
        sb.AppendLine($"- **Validation Level:** {report.Context.Level}");
        sb.AppendLine($"- **Strict Mode:** {(report.Context.StrictMode ? "Enabled" : "Disabled")}");
        sb.AppendLine();

        // 요약
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"**Result:** {(report.IsValid ? "✅ PASSED" : "❌ FAILED")}");
        sb.AppendLine();
        sb.AppendLine("| Severity | Count |");
        sb.AppendLine("|----------|-------|");
        sb.AppendLine($"| Errors | {report.Statistics.ErrorCount} |");
        sb.AppendLine($"| Warnings | {report.Statistics.WarningCount} |");
        sb.AppendLine($"| Info | {report.Statistics.InfoCount} |");
        sb.AppendLine($"| **Total** | **{report.Statistics.TotalIssues}** |");
        sb.AppendLine();

        // 카테고리별 통계
        if (report.Statistics.IssuesByCategory.Count > 0)
        {
            sb.AppendLine("## Issues by Category");
            sb.AppendLine();
            sb.AppendLine("| Category | Count |");
            sb.AppendLine("|----------|-------|");
            foreach (var kvp in report.Statistics.IssuesByCategory.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"| {kvp.Key} | {kvp.Value} |");
            }
            sb.AppendLine();
        }

        // 이슈 상세
        if (report.Issues.Count > 0)
        {
            sb.AppendLine("## Issues Detail");
            sb.AppendLine();

            var groupedIssues = report.Issues.GroupBy(i => i.Severity);
            
            foreach (var group in groupedIssues.OrderBy(g => g.Key))
            {
                var severityTitle = group.Key switch
                {
                    IssueSeverity.Error => "### 🔴 Errors",
                    IssueSeverity.Warning => "### 🟡 Warnings",
                    IssueSeverity.Info => "### 🔵 Information",
                    _ => "### Unknown"
                };

                sb.AppendLine(severityTitle);
                sb.AppendLine();

                foreach (var issue in group)
                {
                    sb.AppendLine($"#### [{issue.Code}] {issue.Message}");
                    
                    if (!string.IsNullOrEmpty(issue.FilePath))
                        sb.AppendLine($"- **File:** `{issue.FilePath}`");
                    
                    if (!string.IsNullOrEmpty(issue.Details))
                        sb.AppendLine($"- **Details:** {issue.Details}");
                    
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                        sb.AppendLine($"- **💡 Suggestion:** {issue.Suggestion}");
                    
                    sb.AppendLine();
                }
            }
        }

        // 검증된 파일 목록
        if (report.Statistics.ValidatedFiles.Count > 0)
        {
            sb.AppendLine("## Validated Files");
            sb.AppendLine();
            foreach (var file in report.Statistics.ValidatedFiles)
            {
                sb.AppendLine($"- `{file}`");
            }
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }
}