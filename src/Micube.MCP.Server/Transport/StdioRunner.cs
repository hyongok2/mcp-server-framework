using System;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Exceptions;
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
        Console.InputEncoding = System.Text.Encoding.UTF8;
        using var reader = new StreamReader(Console.OpenStandardInput());

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                _logger.LogDebug($"[STDIO] Input: {line}");
                await ProcessMessageAsync(line);
            }
            catch (Exception ex)
            {
                _logger.LogError("[STDIO] Error processing message", ex);
            }
        }
    }

    private async Task ProcessMessageAsync(string line)
    {
        // 1. JSON 파싱
        var request = TryParseMessage(line);
        if (request == null) return; // 파싱 실패 시 이미 에러 응답 전송됨

        var requestId = request.Id ?? "unknown";
        _logger.LogInfo($"[STDIO] Processing message: {request.Method}", requestId);

        // 2. 메시지 처리
        var response = await _dispatcher.HandleAsync(request);

        if (response == null)
        {
            _logger.LogDebug("[STDIO] No response generated (notification or unsupported method)", requestId);
            return; // 알림 또는 지원하지 않는 메서드인 경우 응답 없음
        }

        await SendResponseAsync(response);
        _logger.LogDebug("[STDIO] Response sent", requestId);
    }

    private McpMessage? TryParseMessage(string line)
    {
        try
        {
            return JsonConvert.DeserializeObject<McpMessage>(line);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"[STDIO] JSON parsing failed: {ex.Message}");
            SendParseErrorAsync(ex.Message).Wait(); // 동기적으로 에러 전송
            return null;
        }
    }

    private async Task SendParseErrorAsync(string errorDetail)
    {
        var parseError = new McpErrorResponse
        {
            Id = null,
            Error = new McpError
            {
                Code = McpErrorCodes.PARSE_ERROR,
                Message = "Parse error",
                Data = JsonConvert.SerializeObject(errorDetail)
            }
        };

        await Console.Out.WriteLineAsync(JsonConvert.SerializeObject(parseError, _jsonSettings));
        await Console.Out.FlushAsync();
    }

    private async Task SendResponseAsync(object response)
    {
        await Console.Out.WriteLineAsync(JsonConvert.SerializeObject(response, _jsonSettings));
        await Console.Out.FlushAsync();
        _logger.LogDebug($"[STDIO] Output: {JsonConvert.SerializeObject(response)}");
    }
}
