// src/Micube.MCP.Core/Dispatcher/McpMessageDispatcher.cs
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Utils;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Dispatcher;

public class McpMessageDispatcher : IMcpMessageDispatcher
{
    private readonly IMessageValidator _validator;
    private readonly ISessionState _sessionState;
    private readonly Dictionary<string, IMethodHandler> _handlers;
    private readonly IMcpLogger _logger;

    public McpMessageDispatcher(
        IMessageValidator validator,
        ISessionState sessionState,
        IEnumerable<IMethodHandler> handlers,
        IMcpLogger logger)
    {
        _validator = validator;
        _sessionState = sessionState;
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.MethodName, h => h, StringComparer.OrdinalIgnoreCase);

        _logger.LogInfo($"McpMessageDispatcher initialized with {_handlers.Count} handlers:");
        foreach (var handler in _handlers)
        {
            _logger.LogDebug($"  - {handler.Key} ({handler.Value.GetType().Name})");
        }
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        if (message == null)
        {
            _logger.LogError("Received null message");
            return ErrorResponseFactory.Create(null, McpErrorCodes.INVALID_REQUEST,
            "Invalid request", "Message cannot be null");
        }

        var requestId = message.Id ?? "unknown";

        _logger.LogDebug($"Received message: {message.Method}", requestId);

        // 1. 메시지 검증
        var validation = _validator.Validate(message);
        if (!validation.IsValid)
        {
            if (validation.ErrorResponse != null)
            {
                validation.ErrorResponse.Id = message?.Id;
                _logger.LogError($"Message validation failed: {validation.ErrorResponse.Error?.Message}", requestId);
            }
            return validation.ErrorResponse;
        }

        // 2. 핸들러 찾기
        if (!_handlers.TryGetValue(message.Method!, out var handler))
        {
            _logger.LogError($"Method not found: {message.Method}", requestId);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.METHOD_NOT_FOUND,
                "Method not found", message.Method);
        }

        // 3. 초기화 상태 확인
        if (handler.RequiresInitialization && !_sessionState.IsInitialized)
        {
            _logger.LogError($"Method '{message.Method}' called before initialization", requestId);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_REQUEST,
                "Server not initialized", "Call initialize first");
        }

        try
        {
            // 4. 핸들러 실행
            _logger.LogInfo($"Executing method: {message.Method}", requestId);
            var result = await handler.HandleAsync(message);

            if (result == null)
            {
                _logger.LogDebug($"Notification processed: {message.Method}", requestId);
                return null; // 알림의 경우 응답 없음
            }

            _logger.LogInfo($"Method completed: {message.Method}", requestId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception in method '{message.Method}': {ex.Message}", requestId, ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Internal error", ex.Message);
        }
    }
}