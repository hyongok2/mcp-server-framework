using System;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Server.Transport;

public class StdioRunner
{
    private readonly IMcpMessageDispatcher _dispatcher;
    private readonly IMcpLogger _logger;
    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.None
    };

    public StdioRunner(IMcpMessageDispatcher dispatcher, IMcpLogger logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        using var reader = new StreamReader(Console.OpenStandardInput());

        while (cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                _logger.LogDebug($"[STDIO] Input: {line}");

                var request = JsonConvert.DeserializeObject<McpMessage>(line);
                var response = await _dispatcher.HandleAsync(request!);

                await Console.Out.WriteLineAsync(JsonConvert.SerializeObject(response, _jsonSettings));
                await Console.Out.FlushAsync();

                _logger.LogDebug($"[STDIO] Output: {JsonConvert.SerializeObject(response)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("[STDIO] Error processing message", ex);
            }
        }
    }
}
