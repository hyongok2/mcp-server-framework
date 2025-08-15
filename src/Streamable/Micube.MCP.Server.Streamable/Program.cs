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

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Configuration
       .SetBasePath(AppContext.BaseDirectory)
       .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
       .AddEnvironmentVariables();

RegisterServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    var options = app.Services.GetRequiredService<IOptions<StreamableServerOptions>>().Value;
    if (options.EnableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Streamable Server v1");
            c.RoutePrefix = string.Empty;
        });
    }
}

app.UseHttpsRedirection();

var streamableOptions = app.Services.GetRequiredService<IOptions<StreamableServerOptions>>().Value;
if (streamableOptions.EnableCors)
{
    app.UseCors();
}

app.UseRouting();
app.MapControllers();

// Root endpoint - redirect to capabilities
app.MapGet("/", () => Results.Redirect("/mcp/capabilities"));

var logger = app.Services.GetRequiredService<IMcpLogger>();
logger.LogInfo("MCP Streamable Server starting...");

app.Run();

void RegisterServices(IServiceCollection services)
{
    // Configure options
    services.Configure<StreamableServerOptions>(builder.Configuration.GetSection(StreamableServerOptions.SectionName));
    services.Configure<LogFileOptions>(builder.Configuration.GetSection("Logging:File"));
    services.Configure<LogOptions>(builder.Configuration.GetSection("Logging"));

    // Add controllers and API exploration
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MCP Streamable Server",
            Version = "v1",
            Description = "HTTP/SSE MCP Server with streaming support"
        });
    });

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

    services.AddSingleton<IToolQueryService, ToolQueryService>();

    // Handlers - both regular and streaming
    services.AddTransient<IMethodHandler, InitializeHandler>();
    services.AddTransient<IMethodHandler, PingHandler>();
    services.AddTransient<IMethodHandler, InitializedNotificationHandler>();
    services.AddTransient<IMethodHandler, ToolsListHandler>();

    // Replace with stream-compatible tools/call handler
    services.AddTransient<IMethodHandler, ToolsCallStreamHandler>();

    services.AddSingleton<IStreamableToolDispatcher>(sp =>
    {
        var logger = sp.GetRequiredService<IMcpLogger>();
        var toolOptions = sp.GetRequiredService<IOptions<ToolGroupOptions>>().Value;

        var baseDir = AppContext.BaseDirectory;
        var resolvedPath = Path.GetFullPath(Path.Combine(baseDir, toolOptions.Directory));

        var loader = new ToolGroupLoader(logger);
        var groups = loader.LoadFromDirectory(resolvedPath, toolOptions.Whitelist.ToArray());

        return new StreamableToolDispatcher(groups, logger);
    });


    // Streaming services
    services.AddSingleton<IStreamingMessageDispatcher, StreamingMessageDispatcher>();
}

public partial class Program { }
