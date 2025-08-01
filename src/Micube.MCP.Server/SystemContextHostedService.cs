using System;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Options;
using Micube.MCP.Server.Transport;

namespace Micube.MCP.Server;

public class SystemContextHostedService : IHostedService
{
    private readonly IMcpMessageDispatcher _dispatcher;
    private readonly FeatureOptions _features;
    private readonly IMcpLogger _logger;
    public SystemContextHostedService(IMcpMessageDispatcher dispatcher, IMcpLogger logger, IOptions<FeatureOptions> features)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _features = features.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
         _logger.LogInfo("Starting MCP Server Framework");
         
        if (_features.EnableStdio)
        {
            var stdioRunner = new StdioRunner(_dispatcher, _logger);
            return Task.Run(() => stdioRunner.RunAsync(cancellationToken));
        }
        
        _logger.LogInfo("STDIO Runner is disabled by configuration.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("Stopping MCP Server Framework");

        await _logger.ShutdownAsync();
    }
}
