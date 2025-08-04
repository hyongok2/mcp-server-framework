// src/Micube.MCP.Core/Handlers/Core/InitializeHandler.cs (업데이트)
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Handlers.Core;

public class InitializeHandler : IMethodHandler
{
    private readonly ISessionState _sessionState;
    private readonly ICapabilitiesService _capabilitiesService;
    private readonly IMcpLogger _logger;

    public string MethodName => JsonRpcConstants.Methods.Initialize;
    public bool RequiresInitialization => false;

    public InitializeHandler(
        ISessionState sessionState, 
        ICapabilitiesService capabilitiesService,
        IMcpLogger logger)
    {
        _sessionState = sessionState;
        _capabilitiesService = capabilitiesService;
        _logger = logger;
    }

    public async Task<object?> HandleAsync(McpMessage message)
    {
        _logger.LogInfo("[Initialize] Received MCP Server Initialize Request.");

        try
        {
            // 1. 클라이언트 파라미터 파싱
            ClientInitializeParams? clientParams = null;
            if (message.Params != null)
            {
                try
                {
                    clientParams = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(message.Params);
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"[Initialize] Failed to parse client parameters: {ex.Message}");
                    return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_PARAMS, 
                        "Invalid initialization parameters", ex.Message);
                }
            }

            // 2. 기본값 설정 (파라미터가 없는 경우)
            clientParams ??= new ClientInitializeParams
            {
                ProtocolVersion = JsonRpcConstants.ProtocolVersion,
                ClientInfo = new ClientInfo { Name = "Unknown Client", Version = "0.0.0" },
                Capabilities = new ClientCapabilities()
            };

            // 3. 클라이언트 capabilities 검증 및 저장
            var validationResult = _capabilitiesService.ValidateAndStore(clientParams);
            if (!validationResult.IsValid)
            {
                return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INVALID_REQUEST,
                    "Client capabilities validation failed", validationResult.ErrorMessage);
            }

            // 4. 세션 초기화 완료 표시
            _sessionState.MarkAsInitialized();

            // 5. 서버 capabilities 가져오기
            var serverCapabilities = _capabilitiesService.GetServerCapabilities();

            _logger.LogInfo($"[Initialize] Client '{clientParams.ClientInfo.Name}' initialized successfully");

            // 6. 초기화 응답 반환
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
                    capabilities = serverCapabilities
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Initialize] Unexpected error during initialization: {ex.Message}", ex);
            return ErrorResponseFactory.Create(message.Id, McpErrorCodes.INTERNAL_ERROR,
                "Internal server error during initialization", ex.Message);
        }
    }
}