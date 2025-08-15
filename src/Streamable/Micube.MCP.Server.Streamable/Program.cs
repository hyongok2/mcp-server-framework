using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Handlers.Core;
using Micube.MCP.Core.Handlers.Tools;
using Micube.MCP.Core.Loader;
using Micube.MCP.Core.Logging;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Validation;
using Micube.MCP.Core.Streamable.Handlers;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Streamable.Options;
using Micube.MCP.Core.Streamable.Dispatcher;
using Micube.MCP.Core.Streamable.Loader;
using Micube.MCP.Core.Streamable.Services;
using Micube.MCP.Server.Streamable.Services;
using Micube.MCP.Server.Streamable.Services.Health;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Configuration
       .SetBasePath(AppContext.BaseDirectory)
       .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
       .AddEnvironmentVariables();

RegisterServices(builder.Services);

var app = builder.Build();

app.UseHttpsRedirection();

var streamableOptions = app.Services.GetRequiredService<IOptions<StreamableServerOptions>>().Value;
if (streamableOptions.EnableCors)
{
    app.UseCors();
}

app.UseRouting();
app.MapControllers();

var logger = app.Services.GetRequiredService<IMcpLogger>();
logger.LogInfo("MCP Streamable Server starting...");

app.Run();

void RegisterServices(IServiceCollection services)
{
    // Configure options
    services.Configure<StreamableServerOptions>(builder.Configuration.GetSection(StreamableServerOptions.SectionName));
    services.Configure<LogFileOptions>(builder.Configuration.GetSection("Logging:File"));
    services.Configure<LogOptions>(builder.Configuration.GetSection("Logging"));
    services.Configure<ToolGroupOptions>(builder.Configuration.GetSection("ToolGroups"));

    // Add controllers and API exploration
    services.AddControllers();
    services.AddEndpointsApiExplorer();

    // Configure Kestrel
    services.Configure<KestrelServerOptions>(options =>
    {
        var serverOptions = builder.Configuration.GetSection(StreamableServerOptions.SectionName).Get<StreamableServerOptions>()
                          ?? new StreamableServerOptions();

        options.Limits.MaxRequestBodySize = serverOptions.MaxRequestBodySize;
        options.Limits.KeepAliveTimeout = serverOptions.KeepAliveTimeout;
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
    });

    // Add CORS
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Logging services
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

    // Core services
    services.AddSingleton<ICapabilitiesService, CapabilitiesService>();
    services.AddSingleton<IMessageValidator, MessageValidator>();
    services.AddSingleton<ISessionState, SessionState>();

    services.AddSingleton<IToolQueryService, StreamableToolQueryService>();

    // Handlers - both regular and streaming (stateless, so singleton is efficient)
    services.AddSingleton<IMethodHandler, InitializeHandler>();
    services.AddSingleton<IMethodHandler, PingHandler>();
    services.AddSingleton<IMethodHandler, InitializedNotificationHandler>();
    services.AddSingleton<IMethodHandler, ToolsListHandler>();

    // Replace with stream-compatible tools/call handler
    services.AddSingleton<IMethodHandler, ToolsCallStreamHandler>();

    services.AddSingleton<IStreamableToolDispatcher>(sp =>
    {
        var logger = sp.GetRequiredService<IMcpLogger>();
        var toolOptions = sp.GetRequiredService<IOptions<ToolGroupOptions>>().Value;

        var baseDir = AppContext.BaseDirectory;
        var resolvedPath = Path.GetFullPath(Path.Combine(baseDir, toolOptions.Directory));

        var loader = new StreamableToolGroupLoader(logger);
        var groups = loader.LoadFromDirectory(resolvedPath, toolOptions.Whitelist.ToArray());

        return new StreamableToolDispatcher(groups, logger);
    });

    // Streaming services
    services.AddSingleton<IStreamingMessageDispatcher, StreamingMessageDispatcher>();
    
    // Extracted streaming services (SRP compliance)
    services.AddSingleton<IHttpStreamingResponseService, HttpStreamingResponseService>();
    services.AddSingleton<ISseFormatter, SseFormatter>();
    services.AddSingleton<IHeartbeatService, HeartbeatService>();
    services.AddSingleton<ICancellationTokenBuilder, CancellationTokenBuilder>();
    services.AddSingleton<IStreamingResponseCoordinator, StreamingResponseCoordinator>();
    
    // Health services (SRP compliance)
    services.AddSingleton<IComponentHealthChecker, SessionHealthChecker>();
    services.AddSingleton<IComponentHealthChecker, ToolsHealthChecker>();
    services.AddSingleton<IHealthAggregator, HealthAggregator>();
    services.AddSingleton<IHealthResponseFormatter, HealthResponseFormatter>();
}

public partial class Program { }
