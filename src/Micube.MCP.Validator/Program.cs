using System.Reflection;
using System.Text;
using Micube.MCP.Validator.Models;
using Micube.MCP.Validator.Services;
using Micube.MCP.Validator.Constants;
using Spectre.Console;

namespace Micube.MCP.Validator;

class Program
{
    private static FileLogger? _logger;

    static async Task<int> Main(string[] args)
    {
        var exitCode = -1;
        try
        {
            // UTF-8 콘솔 인코딩 설정
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            // 전역 예외 핸들러 설정
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // 로거 초기화 (실행 파일과 같은 디렉토리에 logs 폴더)
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _logger = new FileLogger(logDir);
            _logger.LogInfo("Program", ValidationConstants.Messages.ApplicationStarted, $"Args: [{string.Join(", ", args)}]");

            // 인자 처리
            var enableStreaming = false;
            string? serverUrl = null;
            
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();
                _logger.LogInfo("Program", "Processing argument", arg);
                
                if (arg == "--help" || arg == "-h" || arg == "help")
                {
                    exitCode = await ShowHelp();
                    _logger.LogInfo("Program", ValidationConstants.Messages.HelpDisplayed, $"Exit code: {exitCode}");
                    return exitCode;
                }
                if (arg == "--version" || arg == "-v" || arg == "version")
                {
                    exitCode = await ShowVersion();
                    _logger.LogInfo("Program", ValidationConstants.Messages.VersionDisplayed, $"Exit code: {exitCode}");
                    return exitCode;
                }
                if (arg == "--streaming" || arg == "-s")
                {
                    enableStreaming = true;
                    _logger.LogInfo("Program", "Streaming validation enabled", "Will include streaming validators");
                }
                if (arg == "--server-url" && i + 1 < args.Length)
                {
                    serverUrl = args[++i];
                    _logger.LogInfo("Program", "Server URL specified", serverUrl);
                }
            }

            // 기본 동작: 현재 디렉토리의 모든 MCP 툴 검증
            _logger.LogInfo("Program", "Starting validation", 
                $"Running main validation process (Streaming: {enableStreaming})");
            exitCode = await RunValidation(enableStreaming, serverUrl);
            _logger.LogInfo("Program", ValidationConstants.Messages.ValidationCompleted, $"Exit code: {exitCode}");
            
            return exitCode;
        }
        catch (Exception ex)
        {
            _logger?.LogCritical("Program", "Unhandled exception in Main", "Application crashed", ex);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red bold]❌ CRITICAL ERROR[/]");
            AnsiConsole.MarkupLine("[red]The validator encountered a critical error and cannot continue.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteException(ex);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Check the log file in the 'logs' directory for more details.[/]");
            
            return -1;
        }
        finally
        {
            try
            {
                _logger?.LogInfo("Program", ValidationConstants.Messages.ApplicationEnding, $"Final exit code: {exitCode}");
                _logger?.Dispose();
            }
            catch
            {
                // 로거 정리 중 예외는 무시
            }

            // 사용자가 키를 누를 때까지 대기
            try
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]{ValidationConstants.Messages.PressAnyKeyToExit}[/]");
                Console.ReadKey(true);
            }
            catch
            {
                // ReadKey 실패 시 무시 (예: 파이프라인에서 실행될 때)
            }
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _logger?.LogCritical("UnhandledException", "Unhandled domain exception", 
            $"IsTerminating: {e.IsTerminating}", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogCritical("UnobservedTaskException", "Unobserved task exception", 
            "Background task failed", e.Exception);
        e.SetObserved(); // 예외를 관찰됨으로 표시하여 앱 종료 방지
    }

    static async Task<int> RunValidation(bool enableStreaming = false, string? serverUrl = null)
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            _logger?.LogInfo("RunValidation", "Starting validation process", 
                $"Directory: {currentDir}, Streaming: {enableStreaming}");
            
            PrintLogo();

            var context = CreateValidationContext(currentDir);
            var report = await ExecuteValidation(context, enableStreaming, serverUrl);
            
            if (report == null) return -1;

            GenerateReport(report);
            return GetExitCode(report);
        }
        catch (Exception ex)
        {
            return HandleFatalError(ex);
        }
    }

    private static ValidationContext CreateValidationContext(string currentDir)
    {
        var context = new ValidationContext
        {
            DirectoryPath = currentDir,
            StrictMode = false,
            Level = ValidationLevel.Full
        };

        _logger?.LogInfo("RunValidation", "ValidationContext created", 
            $"Directory: {context.DirectoryPath}, StrictMode: {context.StrictMode}, Level: {context.Level}");

        return context;
    }

    private static async Task<ValidationReport?> ExecuteValidation(
        ValidationContext context, 
        bool enableStreaming = false, 
        string? serverUrl = null)
    {
        try
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync(ValidationConstants.Messages.ValidatingMcpTools, async ctx =>
                {
                    _logger?.LogInfo("RunValidation", "Starting orchestrator validation",
                        $"Streaming: {enableStreaming}");
                    // 스트리밍 검증을 기본적으로 활성화 (서버 URL은 옵션이 있을 때만)
                    var orchestrator = new ValidationOrchestrator(_logger, true, enableStreaming ? serverUrl : null);
                    var result = await orchestrator.ValidateAsync(context);
                    _logger?.LogInfo("RunValidation", "Orchestrator validation completed", 
                        $"IsValid: {result.IsValid}, Issues: {result.Issues.Count}");
                    return result;
                });
        }
        catch (Exception ex)
        {
            HandleValidationError(ex);
            return null;
        }
    }

    private static void HandleValidationError(Exception ex)
    {
        _logger?.LogCritical("RunValidation", "Validation process failed", "Error during validation execution", ex);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red bold]❌ {ValidationConstants.Messages.ValidationFailed}[/]");
        AnsiConsole.MarkupLine("[red]An error occurred during the validation process.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteException(ex);
    }

    private static void GenerateReport(ValidationReport report)
    {
        try
        {
            _logger?.LogInfo("RunValidation", "Generating console report", 
                $"Total issues: {report.Issues.Count}, Errors: {report.Issues.Count(i => i.Severity == IssueSeverity.Error)}");
            
            var reportGenerator = new ReportGenerator();
            reportGenerator.PrintConsoleReport(report);
            
            _logger?.LogInfo("RunValidation", "Console report generated successfully");
        }
        catch (Exception ex)
        {
            HandleReportGenerationError(ex);
        }
    }

    private static void HandleReportGenerationError(Exception ex)
    {
        _logger?.LogError("RunValidation", "Failed to generate console report", "Report generation error", ex);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]❌ Failed to generate report, but validation completed.[/]");
        AnsiConsole.WriteException(ex);
    }

    private static int GetExitCode(ValidationReport report)
    {
        var exitCode = report.IsValid ? 0 : 1;
        _logger?.LogInfo("RunValidation", "Validation process completed", 
            $"ExitCode: {exitCode}, IsValid: {report.IsValid}");
        
        return exitCode;
    }

    private static int HandleFatalError(Exception ex)
    {
        _logger?.LogCritical("RunValidation", "Fatal error in validation process", "Unhandled exception", ex);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red bold]❌ {ValidationConstants.Messages.FatalError}[/]");
        AnsiConsole.MarkupLine("[red]A fatal error occurred during validation.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteException(ex);
        
        return -1;
    }

    static Task<int> ShowVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold yellow]MCP Tool Validator[/] v{informationalVersion}");
        AnsiConsole.MarkupLine("[dim]Copyright (c) 2024 Micube[/]");
        AnsiConsole.WriteLine();

        return Task.FromResult(0);
    }

    static Task<int> ShowHelp()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]MCP Tool Validator[/]");
        AnsiConsole.MarkupLine("[dim]Simple validation utility for MCP tools[/]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]Usage:[/]");
        AnsiConsole.WriteLine("  mcp-validator                              # Validate all MCP tools (includes streaming validation)");
        AnsiConsole.WriteLine("  mcp-validator --streaming --server-url URL # Include live server URL testing");
        AnsiConsole.WriteLine("  mcp-validator --help                       # Show this help");
        AnsiConsole.WriteLine("  mcp-validator --version                    # Show version");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.WriteLine("  --streaming, -s              # Enable live streaming server URL testing");
        AnsiConsole.WriteLine("  --server-url <URL>           # Specify streaming server URL for live testing (default: http://localhost:5556)");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]How it works:[/]");
        AnsiConsole.MarkupLine("  1. Place the [yellow]mcp-validator.exe[/] in your tools directory");
        AnsiConsole.MarkupLine("  2. Ensure DLL and JSON manifest files are in [yellow]same folders[/]");
        AnsiConsole.MarkupLine("  3. Run [yellow]mcp-validator[/] to validate all tools");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Directory Structure:[/]");
        AnsiConsole.WriteLine("  tools/");
        AnsiConsole.WriteLine("  ├── mcp-validator.exe        # This validator");
        AnsiConsole.WriteLine("  ├── MyTool/");
        AnsiConsole.WriteLine("  │   ├── MyTool.dll           # ✅ MCP tool");
        AnsiConsole.WriteLine("  │   └── MyTool.json          # ✅ Manifest (same name preferred)");
        AnsiConsole.WriteLine("  └── OtherTool/");
        AnsiConsole.WriteLine("      ├── OtherTool.dll        # ✅ MCP tool");
        AnsiConsole.WriteLine("      ├── manifest.json        # ✅ Any JSON file works");
        AnsiConsole.WriteLine("      └── dependency.dll       # 🔄 Automatically ignored");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Validation includes:[/]");
        AnsiConsole.MarkupLine("  • JSON manifest structure and required fields");
        AnsiConsole.MarkupLine("  • DLL assembly loading and MCP/Streamable interface implementation");
        AnsiConsole.MarkupLine("  • Manifest-DLL consistency (tool names, parameters)");
        AnsiConsole.MarkupLine("  • Runtime tool instantiation and execution tests");
        AnsiConsole.MarkupLine("  • Streaming capability validation (IAsyncEnumerable<StreamChunk>)");
        AnsiConsole.WriteLine();

        return Task.FromResult(0);
    }


    static void PrintLogo()
    {
        AnsiConsole.WriteLine();
        var figlet = new FigletText("MCP Validator")
            .Color(Color.Yellow);
        AnsiConsole.Write(figlet);
        AnsiConsole.MarkupLine("[dim]Tool validation utility for MCP framework[/]");
        AnsiConsole.WriteLine();
    }
}
