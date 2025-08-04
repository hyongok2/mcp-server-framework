using FluentAssertions;
using Micube.MCP.Core.Handlers.Core;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Tests.TestHelpers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.Core.Tests.Handlers;

public class InitializeHandlerComprehensiveTests
{
    private readonly InitializeHandler _handler;
    private readonly Mock<ISessionState> _sessionStateMock;
    private readonly Mock<ICapabilitiesService> _capabilitiesServiceMock;
    private readonly MockLogger _logger;

    public InitializeHandlerComprehensiveTests()
    {
        _sessionStateMock = new Mock<ISessionState>();
        _capabilitiesServiceMock = new Mock<ICapabilitiesService>();
        _logger = new MockLogger();

        _handler = new InitializeHandler(
            _sessionStateMock.Object,
            _capabilitiesServiceMock.Object,
            _logger);

        // 기본적으로 validation은 성공으로 설정
        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Returns(CapabilitiesValidationResult.Success());
    }

    #region 정상적인 파라미터 테스트

    [Fact]
    public async Task HandleAsync_WithCompleteValidParams_ReturnsSuccess()
    {
        // Arrange
        var clientParams = new ClientInitializeParams
        {
            ProtocolVersion = "2025-06-18",
            ClientInfo = new ClientInfo
            {
                Name = "TestClient",
                Version = "1.0.0",
                Description = "Test client description"
            },
            Capabilities = new ClientCapabilities
            {
                Tools = true,
                Resources = true,
                Prompts = true,
                Sampling = false,
                Logging = true,
                Roots = new RootsCapability { ListChanged = true }
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", clientParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        var response = (McpSuccessResponse)result!;
        response.Id.Should().Be("test-id");
        
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "TestClient" &&
                p.ClientInfo.Version == "1.0.0" &&
                p.Capabilities.Tools == true)), 
            Times.Once);
        
        _sessionStateMock.Verify(x => x.MarkAsInitialized(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMinimalValidParams_ReturnsSuccess()
    {
        // Arrange
        var clientParams = new ClientInitializeParams
        {
            ProtocolVersion = "2025-06-18",
            ClientInfo = new ClientInfo { Name = "MinimalClient", Version = "1.0.0" },
            Capabilities = new ClientCapabilities()
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", clientParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()), Times.Once);
    }

    #endregion

    #region 다양한 JSON 형태의 파라미터 테스트

    [Fact]
    public async Task HandleAsync_WithJObjectParams_ParsesSuccessfully()
    {
        // Arrange - JObject 형태로 파라미터 전달
        var jObject = JObject.FromObject(new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "JObjectClient", version = "1.0.0" },
            capabilities = new { tools = true, resources = false }
        });

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", jObject);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "JObjectClient")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithJsonElementParams_ParsesSuccessfully()
    {
        // Arrange - System.Text.Json JsonElement 형태
        var jsonString = """
        {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "JsonElementClient",
                "version": "2.0.0"
            },
            "capabilities": {
                "tools": true,
                "prompts": true
            }
        }
        """;

        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
        var jsonElement = jsonDoc.RootElement;
        
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", jsonElement);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "JsonElementClient")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDictionaryParams_ParsesSuccessfully()
    {
        // Arrange - Dictionary<string, object> 형태
        var dictParams = new Dictionary<string, object>
        {
            ["protocolVersion"] = "2025-06-18",
            ["clientInfo"] = new Dictionary<string, object>
            {
                ["name"] = "DictClient",
                ["version"] = "1.5.0",
                ["description"] = "Dictionary client"
            },
            ["capabilities"] = new Dictionary<string, object>
            {
                ["tools"] = true,
                ["resources"] = true,
                ["sampling"] = false
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", dictParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "DictClient" &&
                p.ClientInfo.Version == "1.5.0")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithJsonStringParams_ParsesSuccessfully()
    {
        // Arrange - JSON 문자열 형태
        var jsonString = JsonConvert.SerializeObject(new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "JsonStringClient", version = "0.9.0" },
            capabilities = new { tools = false, resources = true }
        });

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", jsonString);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "JsonStringClient")), 
            Times.Once);
    }

    #endregion

    #region 부분적/불완전한 파라미터 테스트

    [Fact]
    public async Task HandleAsync_WithMissingProtocolVersion_UsesDefaults()
    {
        // Arrange - protocolVersion 누락
        var incompleteParams = new
        {
            clientInfo = new { name = "NoProtocolClient", version = "1.0.0" },
            capabilities = new { tools = true }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", incompleteParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "NoProtocolClient" &&
                p.ProtocolVersion == "")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMissingClientInfo_UsesDefaults()
    {
        // Arrange - clientInfo 누락
        var incompleteParams = new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { tools = true, resources = false }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", incompleteParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "" && 
                p.ClientInfo.Version == "")),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMissingCapabilities_UsesDefaults()
    {
        // Arrange - capabilities 누락
        var incompleteParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "NoCapabilitiesClient", version = "1.0.0" }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", incompleteParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.Capabilities != null &&
                p.ClientInfo.Name == "NoCapabilitiesClient")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithPartialCapabilities_FillsDefaults()
    {
        // Arrange - 일부 capabilities만 제공
        var partialParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "PartialCapClient", version = "1.0.0" },
            capabilities = new 
            {
                tools = true,
                // resources, prompts, sampling, logging 누락
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", partialParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.Capabilities.Tools == true &&
                p.Capabilities.Resources == null && // 기본값 (null)
                p.Capabilities.Prompts == null)), // 기본값 (null)
            Times.Once);
    }

    #endregion

    #region 잘못된/손상된 파라미터 테스트

    [Fact]
    public async Task HandleAsync_WithInvalidJsonString_UsesDefaults()
    {
        // Arrange - 잘못된 JSON 문자열
        var invalidJson = "{ invalid json string }";
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", invalidJson);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        
        // 파싱 실패로 기본값 사용됨
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "Unknown Client" &&
                p.ProtocolVersion == "2025-06-18")), 
            Times.Once);

        // 파싱 실패 로그 확인
        _logger.ErrorMessages.Should().Contain(m => m.Message.Contains("Failed to parse client parameters"));
    }

    [Fact]
    public async Task HandleAsync_WithMalformedObject_UsesDefaults()
    {
        // Arrange - 형식이 맞지 않는 객체
        var malformedParams = new
        {
            protocolVersion = 12345, // 문자열이어야 하는데 숫자
            clientInfo = "not an object", // 객체여야 하는데 문자열
            capabilities = new[] { "tools", "resources" } // 객체여야 하는데 배열
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", malformedParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        
        // 파싱은 실패하지만 기본값으로 처리됨
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "Unknown Client")), 
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    public async Task HandleAsync_WithEmptyOrNullStringParams_UsesDefaults(string paramValue)
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", paramValue);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "Unknown Client")), 
            Times.Once);
    }

    #endregion

    #region 특수한 값들 테스트

    [Fact]
    public async Task HandleAsync_WithNullValues_HandlesGracefully()
    {
        // Arrange - null 값들 포함
        var paramsWithNulls = new Dictionary<string, object?>
        {
            ["protocolVersion"] = null,
            ["clientInfo"] = new Dictionary<string, object?>
            {
                ["name"] = null,
                ["version"] = null,
                ["description"] = null
            },
            ["capabilities"] = new Dictionary<string, object?>
            {
                ["tools"] = null,
                ["resources"] = null
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", paramsWithNulls);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithUnicodeClientName_ParsesCorrectly()
    {
        // Arrange - 유니코드 문자 포함
        var unicodeParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new 
            { 
                name = "클라이언트테스트", 
                version = "1.0.0",
                description = "한글 설명입니다 🚀"
            },
            capabilities = new { tools = true }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", unicodeParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "클라이언트테스트")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithVeryLongStrings_HandlesGracefully()
    {
        // Arrange - 매우 긴 문자열
        var longString = new string('A', 10000);
        var longStringParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = longString, version = "1.0.0" },
            capabilities = new { tools = true }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", longStringParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == longString)), 
            Times.Once);
    }

    #endregion

    #region 실제 클라이언트 시나리오 테스트

    [Fact]
    public async Task HandleAsync_WithClaudeClientFormat_ParsesCorrectly()
    {
        // Arrange - Claude 클라이언트 형태의 실제 파라미터
        var claudeParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "Claude Desktop",
                version = "0.7.1"
            },
            capabilities = new
            {
                roots = new { listChanged = true }
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", claudeParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "Claude Desktop" &&
                p.ClientInfo.Version == "0.7.1")), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithVSCodeExtensionFormat_ParsesCorrectly()
    {
        // Arrange - VS Code 확장 형태의 파라미터
        var vscodeParams = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "vscode-mcp",
                version = "1.2.3",
                description = "VS Code MCP Extension"
            },
            capabilities = new
            {
                tools = true,
                resources = true,
                prompts = true,
                logging = true
            }
        };

        var message = TestDataBuilder.CreateMessage("initialize", "test-id", vscodeParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => 
                p.ClientInfo.Name == "vscode-mcp" &&
                p.Capabilities.Tools == true &&
                p.Capabilities.Logging == true)), 
            Times.Once);
    }

    #endregion

    #region 로깅 및 에러 처리 테스트

    [Fact]
    public async Task HandleAsync_WithParsingError_LogsErrorButContinues()
    {
        // Arrange
        var invalidParams = "definitely not json";
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", invalidParams);

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        
        // 에러가 로그되었는지 확인
        _logger.ErrorMessages.Should().Contain(m => 
            m.Message.Contains("Failed to parse client parameters"));
    }

    [Fact]
    public async Task HandleAsync_WithValidationFailure_ReturnsError()
    {
        // Arrange
        var validParams = TestDataBuilder.CreateClientInitializeParams();
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", validParams);

        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Returns(CapabilitiesValidationResult.Error("Validation failed"));

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Message.Should().Contain("validation failed");
    }

    [Fact]
    public async Task HandleAsync_WithException_ReturnsInternalError()
    {
        // Arrange
        var validParams = TestDataBuilder.CreateClientInitializeParams();
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", validParams);

        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Message.Should().Contain("Internal server error");
        
        _logger.ErrorMessages.Should().Contain(m => 
            m.Message.Contains("Unexpected error during initialization"));
    }

    #endregion
}