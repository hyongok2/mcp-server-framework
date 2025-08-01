using System;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Handlers.Core;

public class InitializedNotificationHandler : IMethodHandler
{
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.NotificationsInitialized;
    public bool RequiresInitialization => false;

    public InitializedNotificationHandler(IMcpLogger logger)
    {
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("[Initialized] Client initialization completed.");
        
        return await Task.FromResult<object?>(null); // 알림이므로 응답 없음
    }
}