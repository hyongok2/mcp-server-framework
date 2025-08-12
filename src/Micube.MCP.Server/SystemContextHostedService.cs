using System;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Options;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Options;
using Micube.MCP.Server.Transport;

namespace Micube.MCP.Server;

public class SystemContextHostedService : IHostedService
{
    private readonly IMcpMessageDispatcher _dispatcher;
    private readonly FeatureOptions _features;
    private readonly ToolGroupOptions _toolGroupOptions;
    private readonly ResourceOptions _resourceOptions;
    private readonly PromptOptions _promptOptions;
    private readonly LogOptions _logOptions;
    private readonly IMcpLogger _logger;
    private readonly IConfigurationValidator _configValidator;
    private CancellationTokenSource? _cancellationTokenSource;

    public SystemContextHostedService(
        IMcpMessageDispatcher dispatcher, 
        IMcpLogger logger, 
        IOptions<FeatureOptions> features,
        IOptions<ToolGroupOptions> toolGroupOptions,
        IOptions<ResourceOptions> resourceOptions,
        IOptions<PromptOptions> promptOptions,
        IOptions<LogOptions> logOptions,
        IConfigurationValidator configValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
        _features = features.Value;
        _toolGroupOptions = toolGroupOptions.Value;
        _resourceOptions = resourceOptions.Value;
        _promptOptions = promptOptions.Value;
        _logOptions = logOptions.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("=== MCP Server Framework Starting ===");
        
        try
        {
            // 1. 설정 검증 및 초기화 (가장 먼저 실행)
            _configValidator.ValidateAndSetup(
                _toolGroupOptions,
                _resourceOptions, 
                _promptOptions,
                _logOptions,
                _features,
                _logger);

            _logger.LogInfo("✅ Configuration validation completed");

            // 2. STDIO 서비스 시작
            if (_features.EnableStdio)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var stdioRunner = new StdioRunner(_dispatcher, _logger);
                
                // STDIO 실행을 백그라운드 태스크로 시작
                _ = Task.Run(() => stdioRunner.RunAsync(_cancellationTokenSource.Token));
                
                _logger.LogInfo("✅ STDIO transport enabled and started");
            }
            else
            {
                _logger.LogInfo("⚠️  STDIO transport disabled by configuration");
            }

            // 3. HTTP 서비스 상태 로깅
            if (_features.EnableHttp)
            {
                _logger.LogInfo("✅ HTTP transport enabled");
            }
            else
            {
                _logger.LogInfo("⚠️  HTTP transport disabled by configuration");
            }

            _logger.LogInfo("🚀 MCP Server Framework started successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Failed to start MCP Server Framework: {ex.Message}", ex);
            throw; // 설정 오류 시 애플리케이션 시작 중단
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("=== MCP Server Framework Stopping ===");

        try
        {
            // STDIO 서비스 중지
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _logger.LogInfo("✅ STDIO transport stopped");
            }

            _logger.LogInfo("✅ MCP Server Framework stopped gracefully");
            
            // 로거 종료 (모든 로그가 플러시되도록)
            await _logger.ShutdownAsync();
            
        }
        catch (Exception ex)
        {
            // 종료 시에는 로깅만 하고 예외를 던지지 않음
            _logger.LogError($"⚠️  Error during shutdown: {ex.Message}", ex);
        }
    }
}
