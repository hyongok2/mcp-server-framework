using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Handlers.Core;

public class InitializeHandler : IMethodHandler
{
    private readonly ISessionState _sessionState;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.Initialize;
    public bool RequiresInitialization => false;

    public InitializeHandler(ISessionState sessionState, IMcpLogger logger)
    {
        _sessionState = sessionState;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("[Initialize] Received MCP Server Initialize Request.");

        // 클라이언트 capabilities 검증
        if (message.Params != null)
        {
            var clientInfo = JsonConvert.DeserializeObject<dynamic>(message.Params.ToString() ?? "{}");
            _logger.LogInfo($"[Initialize] Client info: {JsonConvert.SerializeObject(clientInfo)}");
        }

        _sessionState.MarkAsInitialized();

        return await Task.FromResult(new McpSuccessResponse
        {
            Id = message.Id,
            Result = new
            {
                protocolVersion = JsonRpcConstants.ProtocolVersion,
                serverInfo = new
                {
                    name = JsonRpcConstants.ServerInfo.Name,
                    version = JsonRpcConstants.ServerInfo.Version,
                    description = JsonRpcConstants.ServerInfo.Description
                },
                capabilities = new
                {
                    tools = new { },
                    logging = new { }
                }
            }
        });
    }
}

