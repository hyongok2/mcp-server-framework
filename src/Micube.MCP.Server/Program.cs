using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Handlers.Core;
using Micube.MCP.Core.Handlers.Prompts;
using Micube.MCP.Core.Handlers.Resources;
using Micube.MCP.Core.Handlers.Tools;
using Micube.MCP.Core.Loader;
using Micube.MCP.Core.Logging;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Services.Tool;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server;
using Micube.MCP.Server.Middleware;
using Micube.MCP.Server.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Configuration
       .SetBasePath(AppContext.BaseDirectory)
       .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
       .AddEnvironmentVariables();

RegisterServices(builder.Services);

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();


void RegisterServices(IServiceCollection services)
{
    services.Configure<LogFileOptions>(builder.Configuration.GetSection("Logging:File"));
    services.Configure<ToolGroupOptions>(builder.Configuration.GetSection("ToolGroups"));
    services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));
    services.Configure<LogOptions>(builder.Configuration.GetSection("Logging"));
    services.Configure<ResourceOptions>(builder.Configuration.GetSection("Resources"));
    services.Configure<PromptOptions>(builder.Configuration.GetSection("Prompts"));

    services.AddHostedService<SystemContextHostedService>();
    services.AddControllers();

    services.AddSingleton<ILogWriter>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<LogFileOptions>>().Value;
        return new FileLogWriter(
            options.Directory,
            options.FlushIntervalSeconds,
            options.MaxFileSizeMB,
            options.RetentionDays
        );
    });

    services.AddSingleton<IMcpLogger>(sp =>
    {
        var writers = sp.GetServices<ILogWriter>();

        var logOptions = sp.GetRequiredService<IOptions<LogOptions>>().Value;
        var levelStr = logOptions.MinLevel;

        var level = Enum.TryParse<Micube.MCP.Core.Logging.LogLevel>(levelStr, true, out var parsed)
                    ? parsed
                    : Micube.MCP.Core.Logging.LogLevel.Info;

        return new LogDispatcher(writers, level);
    });

    services.AddSingleton<IResourceService>(sp =>
    {
        var logger = sp.GetRequiredService<IMcpLogger>();
        var resourceOptions = sp.GetRequiredService<IOptions<ResourceOptions>>().Value;
        return new ResourceService(logger, resourceOptions);
    });

    services.AddSingleton<IPromptService>(sp =>
    {
        var logger = sp.GetRequiredService<IMcpLogger>();
        var promptOptions = sp.GetRequiredService<IOptions<PromptOptions>>().Value;
        return new PromptService(logger, promptOptions);
    });

    services.AddSingleton<ICapabilitiesService, CapabilitiesService>();
    services.AddSingleton<IMessageValidator, MessageValidator>();
    services.AddSingleton<ISessionState, SessionState>();
    services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();

    // 핸들러들 등록 (stateless이므로 singleton이 효율적)
    services.AddSingleton<IMethodHandler, InitializeHandler>();
    services.AddSingleton<IMethodHandler, PingHandler>();
    services.AddSingleton<IMethodHandler, InitializedNotificationHandler>();
    services.AddSingleton<IMethodHandler, ToolsListHandler>();
    services.AddSingleton<IMethodHandler, ToolsCallHandler>();
    services.AddSingleton<IMethodHandler, ResourcesListHandler>();
    services.AddSingleton<IMethodHandler, ResourcesReadHandler>();
    services.AddSingleton<IMethodHandler, PromptsListHandler>();
    services.AddSingleton<IMethodHandler, PromptsGetHandler>();

    services.AddSingleton<IToolNameParser, ToolNameParser>();

    services.AddSingleton<IToolQueryService, ToolQueryService>();
    services.AddSingleton<IToolDispatcher>(sp =>
    {
        var logger = sp.GetRequiredService<IMcpLogger>();
        var toolNameParser = sp.GetRequiredService<IToolNameParser>();
        var toolOptions = sp.GetRequiredService<IOptions<ToolGroupOptions>>().Value;

        var baseDir = AppContext.BaseDirectory;
        var resolvedPath = Path.GetFullPath(Path.Combine(baseDir, toolOptions.Directory));

        var loader = new ToolGroupLoader(logger);
        var groups = loader.LoadFromDirectory(resolvedPath, toolOptions.Whitelist.ToArray());

        return new ToolDispatcher(groups, logger,toolNameParser);
    });
    services.AddSingleton<IMcpMessageDispatcher, McpMessageDispatcher>();

}

public partial class Program { }
