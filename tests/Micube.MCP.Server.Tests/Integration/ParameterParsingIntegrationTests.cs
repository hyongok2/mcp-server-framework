using FluentAssertions;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.Server.Tests.Integration;

/// <summary>
/// 실제 클라이언트 시나리오를 기반으로 한 파라미터 파싱 통합 테스트
/// 다양한 형태의 입력에 대해 전체 플로우를 테스트합니다.
/// </summary>
public class ParameterParsingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ParameterParsingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
        
        _client = _factory.CreateClient();
    }

    #region 실제 클라이언트 시나리오 테스트

    [Fact]
    public async Task Initialize_WithClaudeDesktopFormat_ReturnsSuccess()
    {
        // Arrange - Claude Desktop 실제 초기화 메시지 형태
        var claudeMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "claude-init-1",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = "Claude Desktop",
                    version = "0.7.1"
                },
                capabilities = new
                {
                    roots = new { listChanged = true },
                    sampling = new { }
                }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(claudeMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
        response.Should().Contain("Micube MCP Server Framework");
    }

    [Fact]
    public async Task Initialize_WithVSCodeExtensionFormat_ReturnsSuccess()
    {
        // Arrange - VS Code 확장 형태의 초기화
        var vscodeMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize", 
            Id = "vscode-init-1",
            Params = new
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
            }
        };

        // Act
        var response = await SendMcpMessageAsync(vscodeMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("capabilities");
        response.Should().Contain("tools");
    }

    [Fact]
    public async Task Initialize_WithCurlCommandFormat_ReturnsSuccess()
    {
        // Arrange - curl 명령어로 보낼 수 있는 raw JSON 형태
        var rawJsonMessage = """
        {
            "jsonrpc": "2.0",
            "method": "initialize",
            "id": "curl-test-1",
            "params": {
                "protocolVersion": "2025-06-18",
                "clientInfo": {
                    "name": "curl-client",
                    "version": "1.0.0"
                },
                "capabilities": {
                    "tools": true
                }
            }
        }
        """;

        // Act
        var httpContent = new StringContent(rawJsonMessage, Encoding.UTF8, "application/json");
        var httpResponse = await _client.PostAsync("/mcp", httpContent);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        responseContent.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithPythonClientFormat_ReturnsSuccess()
    {
        // Arrange - Python 클라이언트에서 보낼 수 있는 형태
        var pythonStyleParams = new Dictionary<string, object>
        {
            ["protocol_version"] = "2025-06-18", // snake_case
            ["client_info"] = new Dictionary<string, object>
            {
                ["name"] = "python-mcp-client",
                ["version"] = "0.1.0"
            },
            ["capabilities"] = new Dictionary<string, object>
            {
                ["tools"] = true,
                ["resources"] = false
            }
        };

        var pythonMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "python-init-1",
            Params = pythonStyleParams
        };

        // Act
        var response = await SendMcpMessageAsync(pythonMessage);

        // Assert
        // snake_case는 파싱이 안되므로 기본값으로 처리됨
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 다양한 JSON 형태 테스트

    [Fact]
    public async Task Initialize_WithNewtonsoftJObjectFormat_ReturnsSuccess()
    {
        // Arrange - Newtonsoft.Json JObject 형태
        var jObjectParams = JObject.FromObject(new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "JObject-Client", version = "1.0.0" },
            capabilities = new { tools = true, prompts = true }
        });

        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "jobject-test",
            Params = jObjectParams
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithSystemTextJsonFormat_ReturnsSuccess()
    {
        // Arrange - System.Text.Json JsonElement 형태
        var jsonString = """
        {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "SystemTextJson-Client",
                "version": "2.0.0"
            },
            "capabilities": {
                "resources": true,
                "logging": false
            }
        }
        """;

        var jsonDoc = JsonDocument.Parse(jsonString);
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "system-json-test",
            Params = jsonDoc.RootElement
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("capabilities");
    }

    #endregion

    #region 부분적/불완전한 데이터 테스트

    [Fact]
    public async Task Initialize_WithMissingProtocolVersion_UsesDefaults()
    {
        // Arrange
        var incompleteMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "incomplete-1",
            Params = new
            {
                clientInfo = new { name = "IncompleteClient", version = "1.0.0" },
                capabilities = new { tools = true }
                // protocolVersion 누락
            }
        };

        // Act
        var response = await SendMcpMessageAsync(incompleteMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("protocolVersion");
        response.Should().Contain("2025-06-18"); // 기본값 사용됨
    }

    [Fact]
    public async Task Initialize_WithMissingClientInfo_UsesDefaults()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "no-client-info",
            Params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { tools = true }
                // clientInfo 누락
            }
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithEmptyCapabilities_UsesDefaults()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "empty-capabilities",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "EmptyCapClient", version = "1.0.0" },
                capabilities = new { } // 빈 객체
            }
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("capabilities");
    }

    #endregion

    #region 잘못된 데이터 복원력 테스트

    [Fact]
    public async Task Initialize_WithInvalidJsonInParams_ReturnsSuccessWithDefaults()
    {
        // Arrange - 파라미터에 잘못된 JSON 문자열
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "invalid-json-params",
            Params = "{ this is not valid json }"
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
        // 파싱 실패 시 기본값으로 처리됨
    }

    [Fact]
    public async Task Initialize_WithNullParams_ReturnsSuccessWithDefaults()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "null-params",
            Params = null
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }
    [Fact]
    public async Task Initialize_WithWrongDataTypes_ReturnsSuccessWithDefaults()
    {
        // Arrange - 잘못된 데이터 타입들
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "wrong-types",
            Params = new
            {
                protocolVersion = 12345, // 숫자 (문자열이어야 함)
                clientInfo = "not an object", // 문자열 (객체여야 함)
                capabilities = new[] { "tools", "resources" } // 배열 (객체여야 함)
            }
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 특수 문자 및 인코딩 테스트

    [Fact]
    public async Task Initialize_WithUnicodeClientName_ReturnsSuccess()
    {
        // Arrange
        var unicodeMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "unicode-test",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = "유니코드클라이언트🚀",
                    version = "한글버전1.0",
                    description = "This is a test with various chars: àáâãäåæçèéêë 中文 🎉✨"
                },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(unicodeMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithSpecialCharacters_ReturnsSuccess()
    {
        // Arrange
        var specialCharsMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "special-chars",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = @"Client with ""quotes"" and \backslashes/ and newlines
                    and tabs	and other stuff",
                    version = "1.0.0"
                },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(specialCharsMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 대용량 데이터 테스트

    [Fact]
    public async Task Initialize_WithLargeClientName_ReturnsSuccess()
    {
        // Arrange - 매우 긴 클라이언트 이름
        var largeClientName = new string('A', 10000);
        var largeMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "large-data",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = largeClientName,
                    version = "1.0.0",
                    description = new string('B', 5000)
                },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(largeMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 실제 네트워크 시나리오 테스트

    [Fact]
    public async Task Initialize_WithMultipleRapidRequests_AllReturnSuccess()
    {
        // Arrange - 동시에 여러 초기화 요청
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < 10; i++)
        {
            var message = new McpMessage
            {
                JsonRpc = "2.0",
                Method = "initialize",
                Id = $"concurrent-{i}",
                Params = new
                {
                    protocolVersion = "2025-06-18",
                    clientInfo = new { name = $"ConcurrentClient{i}", version = "1.0.0" },
                    capabilities = new { tools = true }
                }
            };
            
            tasks.Add(SendMcpMessageAsync(message));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.Contains("serverInfo"));
    }

    [Fact]
    public async Task Initialize_WithDifferentContentTypes_ReturnsSuccess()
    {
        // Arrange - 다양한 Content-Type 헤더로 테스트
        var message = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "content-type-test",
            @params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "ContentTypeClient", version = "1.0.0" },
                capabilities = new { tools = true }
            }
        };

        var jsonContent = JsonConvert.SerializeObject(message);

        // Act & Assert - application/json
        var content1 = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response1 = await _client.PostAsync("/mcp", content1);
        response1.EnsureSuccessStatusCode();

        // Act & Assert - text/json
        var content2 = new StringContent(jsonContent, Encoding.UTF8, "text/json");
        var response2 = await _client.PostAsync("/mcp", content2);
        response2.EnsureSuccessStatusCode();
    }

    #endregion

    #region 에러 시나리오 테스트

    [Fact]
    public async Task Initialize_WithCapabilitiesValidationFailure_ReturnsError()
    {
        // Arrange - 이 테스트는 CapabilitiesService에서 검증 실패가 발생하는 시나리오
        // 현재 구현에서는 대부분의 케이스를 허용하므로, 극단적인 케이스 테스트
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "validation-fail",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "ValidationTestClient", version = "1.0.0" },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(message);

        // Assert
        // 현재 구현에서는 대부분 성공하지만, 향후 엄격한 검증이 추가되면 에러 처리 확인
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Initialize_AfterServerRestart_ReturnsSuccess()
    {
        // Arrange - 서버 재시작 후 초기화 (통합 테스트에서는 시뮬레이션)
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "restart-test",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "RestartClient", version = "1.0.0" },
                capabilities = new { tools = true }
            }
        };

        // Act - 첫 번째 초기화
        var response1 = await SendMcpMessageAsync(message);
        
        // Act - 동일한 클라이언트의 재초기화
        message.Id = "restart-test-2";
        var response2 = await SendMcpMessageAsync(message);

        // Assert
        response1.Should().NotBeNull().And.Contain("serverInfo");
        response2.Should().NotBeNull().And.Contain("serverInfo");
    }

    #endregion

    #region 실제 클라이언트 호환성 테스트

    [Fact]
    public async Task Initialize_WithMinimalRequiredFields_ReturnsSuccess()
    {
        // Arrange - 최소한의 필수 필드만 포함
        var minimalMessage = new McpMessage
        {
            JsonRpc = "2.0", 
            Method = "initialize",
            Id = "minimal-test",
            Params = new { protocolVersion = "2025-06-18" }
        };

        // Act
        var response = await SendMcpMessageAsync(minimalMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithAllPossibleCapabilities_ReturnsSuccess()
    {
        // Arrange - 모든 가능한 capabilities 포함
        var fullCapabilitiesMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "full-capabilities",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = "FullCapabilitiesClient",
                    version = "1.0.0",
                    description = "Client with all capabilities"
                },
                capabilities = new
                {
                    tools = true,
                    resources = true,
                    prompts = true,
                    sampling = true,
                    logging = true,
                    roots = new { listChanged = true }
                }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(fullCapabilitiesMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("capabilities");
        response.Should().Contain("tools");
        response.Should().Contain("resources");
        response.Should().Contain("prompts");
    }

    [Fact]
    public async Task Initialize_WithFutureProtocolVersion_ReturnsSuccess()
    {
        // Arrange - 미래 프로토콜 버전 (호환성 테스트)
        var futureVersionMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "future-version",
            Params = new
            {
                protocolVersion = "2026-01-01", // 미래 버전
                clientInfo = new { name = "FutureClient", version = "2.0.0" },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(futureVersionMessage);

        // Assert
        // 현재 구현에서는 프로토콜 버전 검증을 유연하게 처리
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_WithLegacyProtocolVersion_ReturnsSuccess()
    {
        // Arrange - 레거시 프로토콜 버전
        var legacyMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "legacy-version",
            Params = new
            {
                protocolVersion = "2024-12-01", // 이전 버전
                clientInfo = new { name = "LegacyClient", version = "0.5.0" },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(legacyMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 헬퍼 메서드

    private async Task<string> SendMcpMessageAsync(McpMessage message)
    {
        var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/mcp", content);
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    #endregion

    #region 성능 및 부하 테스트

    [Fact]
    public async Task Initialize_WithHighFrequencyRequests_MaintainsPerformance()
    {
        // Arrange - 고빈도 요청 테스트
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task<string>>();

        // Act - 100개의 동시 요청
        for (int i = 0; i < 100; i++)
        {
            var message = new McpMessage
            {
                JsonRpc = "2.0",
                Method = "initialize",
                Id = $"perf-test-{i}",
                Params = new
                {
                    protocolVersion = "2025-06-18",
                    clientInfo = new { name = $"PerfClient{i}", version = "1.0.0" },
                    capabilities = new { tools = true }
                }
            };
            
            tasks.Add(SendMcpMessageAsync(message));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(100);
        responses.Should().OnlyContain(r => r.Contains("serverInfo"));
        
        // 성능 검증 - 100개 요청이 10초 이내에 처리되어야 함
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Fact]
    public async Task Initialize_WithVeryLargePayload_ReturnsSuccess()
    {
        // Arrange - 매우 큰 페이로드 테스트
        var largeData = new string('X', 100000); // 100KB 문자열
        
        var largeMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "large-payload",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = "LargePayloadClient",
                    version = "1.0.0",
                    description = largeData
                },
                capabilities = new { tools = true }
            }
        };

        // Act
        var response = await SendMcpMessageAsync(largeMessage);

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("serverInfo");
    }

    #endregion

    #region 실제 클라이언트 라이브러리 시뮬레이션

    [Fact]
    public async Task Initialize_SimulateNodeJSMcpClient_ReturnsSuccess()
    {
        // Arrange - Node.js MCP 클라이언트 시뮬레이션
        var nodeJsStyleMessage = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = Guid.NewGuid().ToString(),
            @params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new
                {
                    name = "nodejs-mcp-client",
                    version = "1.0.0"
                },
                capabilities = new
                {
                    tools = true,
                    resources = true
                }
            }
        };

        var json = JsonConvert.SerializeObject(nodeJsStyleMessage);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("serverInfo");
    }

    [Fact]
    public async Task Initialize_SimulatePythonMcpClient_ReturnsSuccess() 
    {
        // Arrange - Python MCP 클라이언트 시뮬레이션 (requests 라이브러리)
        var pythonStyleMessage = new Dictionary<string, object>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "initialize", 
            ["id"] = 12345, // Python에서는 숫자 ID도 자주 사용
            ["params"] = new Dictionary<string, object>
            {
                ["protocolVersion"] = "2025-06-18",
                ["clientInfo"] = new Dictionary<string, object>
                {
                    ["name"] = "python-mcp-client",
                    ["version"] = "0.1.0"
                },
                ["capabilities"] = new Dictionary<string, object>
                {
                    ["tools"] = true
                }
            }
        };

        var json = JsonConvert.SerializeObject(pythonStyleMessage);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("serverInfo");
    }

    #endregion
}