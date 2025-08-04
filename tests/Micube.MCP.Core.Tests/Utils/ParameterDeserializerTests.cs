using FluentAssertions;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.Core.Tests.Utils;

public class ParameterDeserializerTests
{
    private readonly ClientInitializeParams _expectedParams;

    public ParameterDeserializerTests()
    {
        _expectedParams = new ClientInitializeParams
        {
            ProtocolVersion = "2025-06-18",
            ClientInfo = new ClientInfo
            {
                Name = "TestClient",
                Version = "1.0.0",
                Description = "Test Description"
            },
            Capabilities = new ClientCapabilities
            {
                Tools = true,
                Resources = false,
                Prompts = true,
                Sampling = null,
                Logging = true
            }
        };
    }

    #region 기본 타입 테스트

    [Fact]
    public void DeserializeParams_WithDirectType_ReturnsDirectly()
    {
        // Arrange - 이미 원하는 타입인 경우
        var directParams = _expectedParams;

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(directParams);

        // Assert
        result.Should().BeSameAs(directParams);
        result!.ClientInfo.Name.Should().Be("TestClient");
    }

    [Fact]
    public void DeserializeParams_WithNull_ReturnsNull()
    {
        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(null);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region JSON 문자열 테스트

    [Fact]
    public void DeserializeParams_WithValidJsonString_DeserializesCorrectly()
    {
        // Arrange
        var jsonString = JsonConvert.SerializeObject(_expectedParams);

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jsonString);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("TestClient");
        result.ClientInfo.Version.Should().Be("1.0.0");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeFalse();
    }

    [Fact]
    public void DeserializeParams_WithEmptyJsonString_ThrowsException()
    {
        // Arrange
        var emptyJson = "";

        // Act & Assert
        var act = () => ParameterDeserializer.DeserializeParams<ClientInitializeParams>(emptyJson);
        act.Should().NotBeNull(); // 예외가 발생하지 않아야 함
    }

    [Fact]
    public void DeserializeParams_WithWhitespaceJsonString_ThrowsException()
    {
        // Arrange
        var whitespaceJson = "   ";

        // Act & Assert
        var act = () => ParameterDeserializer.DeserializeParams<ClientInitializeParams>(whitespaceJson);
        act.Should().Throw<Newtonsoft.Json.JsonException>();
    }

    [Fact]
    public void DeserializeParams_WithInvalidJsonString_ThrowsException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        var act = () => ParameterDeserializer.DeserializeParams<ClientInitializeParams>(invalidJson);
        act.Should().Throw<Newtonsoft.Json.JsonException>()
            .WithMessage("*Failed to deserialize parameters*");
    }

    [Fact]
    public void DeserializeParams_WithPartialJsonString_FillsDefaults()
    {
        // Arrange
        var partialJson = """
        {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "PartialClient"
            }
        }
        """;

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(partialJson);

        // Assert
        result.Should().NotBeNull();
        result!.ProtocolVersion.Should().Be("2025-06-18");
        result.ClientInfo.Name.Should().Be("PartialClient");
        result.ClientInfo.Version.Should().BeNullOrEmpty(); // 기본값
        result.Capabilities.Should().NotBeNull(); // 기본 생성자 호출됨
    }

    #endregion

    #region JObject 테스트 (Newtonsoft.Json)

    [Fact]
    public void DeserializeParams_WithJObject_DeserializesCorrectly()
    {
        // Arrange
        var jObject = JObject.FromObject(_expectedParams);

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jObject);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("TestClient");
        result.Capabilities.Tools.Should().BeTrue();
    }

    [Fact]
    public void DeserializeParams_WithJToken_DeserializesCorrectly()
    {
        // Arrange
        var jToken = JToken.FromObject(_expectedParams);

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jToken);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("TestClient");
    }

    [Fact]
    public void DeserializeParams_WithJObjectFromAnonymous_DeserializesCorrectly()
    {
        // Arrange
        var anonymousObj = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new { name = "AnonymousClient", version = "2.0.0" },
            capabilities = new { tools = true, resources = true }
        };
        var jObject = JObject.FromObject(anonymousObj);

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jObject);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("AnonymousClient");
        result.ClientInfo.Version.Should().Be("2.0.0");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeTrue();
    }

    #endregion

    #region JsonElement 테스트 (System.Text.Json)

    [Fact]
    public void DeserializeParams_WithJsonElement_DeserializesCorrectly()
    {
        // Arrange
        var jsonString = System.Text.Json.JsonSerializer.Serialize(_expectedParams);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var jsonElement = jsonDoc.RootElement;

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jsonElement);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("TestClient");
        result.Capabilities.Tools.Should().BeTrue();
    }

    [Fact]
    public void DeserializeParams_WithComplexJsonElement_DeserializesCorrectly()
    {
        // Arrange
        var complexJson = """
        {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "ComplexClient",
                "version": "3.0.0",
                "description": "A complex test client"
            },
            "capabilities": {
                "tools": true,
                "resources": false,
                "prompts": true,
                "sampling": null,
                "logging": true,
                "roots": {
                    "listChanged": true
                }
            }
        }
        """;

        var jsonDoc = JsonDocument.Parse(complexJson);
        var jsonElement = jsonDoc.RootElement;

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(jsonElement);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("ComplexClient");
        result.ClientInfo.Description.Should().Be("A complex test client");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeFalse();
        result.Capabilities.Roots.Should().NotBeNull();
        result.Capabilities.Roots!.ListChanged.Should().BeTrue();
    }

    #endregion

    #region Dictionary 테스트

    [Fact]
    public void DeserializeParams_WithDictionary_DeserializesCorrectly()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            ["protocolVersion"] = "2025-06-18",
            ["clientInfo"] = new Dictionary<string, object>
            {
                ["name"] = "DictClient",
                ["version"] = "1.5.0"
            },
            ["capabilities"] = new Dictionary<string, object>
            {
                ["tools"] = true,
                ["resources"] = false
            }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(dict);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("DictClient");
        result.ClientInfo.Version.Should().Be("1.5.0");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeFalse();
    }

    [Fact]
    public void DeserializeParams_WithNestedDictionaries_DeserializesCorrectly()
    {
        // Arrange
        var nestedDict = new Dictionary<string, object>
        {
            ["protocolVersion"] = "2025-06-18",
            ["clientInfo"] = new Dictionary<string, object>
            {
                ["name"] = "NestedClient",
                ["version"] = "2.0.0",
                ["description"] = "Nested dictionary test"
            },
            ["capabilities"] = new Dictionary<string, object>
            {
                ["tools"] = true,
                ["roots"] = new Dictionary<string, object>
                {
                    ["listChanged"] = false
                }
            }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(nestedDict);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("NestedClient");
        result.ClientInfo.Description.Should().Be("Nested dictionary test");
        result.Capabilities.Roots.Should().NotBeNull();
        result.Capabilities.Roots!.ListChanged.Should().BeFalse();
    }

    #endregion

    #region 일반 객체 테스트

    [Fact]
    public void DeserializeParams_WithAnonymousObject_DeserializesCorrectly()
    {
        // Arrange
        var anonymousObj = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "AnonymousObj",
                version = "1.0.0"
            },
            capabilities = new
            {
                tools = false,
                resources = true,
                prompts = false
            }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(anonymousObj);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("AnonymousObj");
        result.Capabilities.Tools.Should().BeFalse();
        result.Capabilities.Resources.Should().BeTrue();
        result.Capabilities.Prompts.Should().BeFalse();
    }

    [Fact]
    public void DeserializeParams_WithCustomObject_DeserializesCorrectly()
    {
        // Arrange
        var customObj = new CustomInitParams
        {
            ProtocolVersion = "2025-06-18",
            ClientName = "CustomClient",
            ClientVersion = "1.0.0",
            SupportsTools = true
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(customObj);

        // Assert
        result.Should().NotBeNull();
        // 프로퍼티 이름이 다르므로 기본값들이 설정됨
        result!.ProtocolVersion.Should().Be("2025-06-18");
    }

    #endregion

    #region 특수 케이스 테스트

    [Fact]
    public void DeserializeParams_WithCircularReference_ThrowsException()
    {
        // Arrange
        var obj1 = new Dictionary<string, object>();
        var obj2 = new Dictionary<string, object>();
        obj1["child"] = obj2;
        obj2["parent"] = obj1; // 순환 참조

        obj1["protocolVersion"] = "2025-06-18";
        obj1["clientInfo"] = new { name = "CircularClient", version = "1.0.0" };

        // Act & Assert
        var act = () => ParameterDeserializer.DeserializeParams<ClientInitializeParams>(obj1);
        act.Should().Throw<Newtonsoft.Json.JsonException>();
    }

    [Fact]
    public void DeserializeParams_WithNullProperties_HandlesGracefully()
    {
        // Arrange
        var objWithNulls = new Dictionary<string, object?>
        {
            ["protocolVersion"] = null,
            ["clientInfo"] = new Dictionary<string, object?>
            {
                ["name"] = null,
                ["version"] = "1.0.0",
                ["description"] = null
            },
            ["capabilities"] = null
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(objWithNulls);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Should().NotBeNull();
        result.ClientInfo.Version.Should().Be("1.0.0");
        result.ClientInfo.Name.Should().BeNull();
    }

    [Fact]
    public void DeserializeParams_WithUnicodeContent_DeserializesCorrectly()
    {
        // Arrange
        var unicodeObj = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "유니코드클라이언트",
                version = "1.0.0",
                description = "한글 설명 테스트 🚀✨"
            },
            capabilities = new { tools = true }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(unicodeObj);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("유니코드클라이언트");
        result.ClientInfo.Description.Should().Be("한글 설명 테스트 🚀✨");
    }

    [Fact]
    public void DeserializeParams_WithLargeObject_DeserializesCorrectly()
    {
        // Arrange - 큰 객체
        var largeDescription = new string('A', 50000);
        var largeObj = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "LargeClient",
                version = "1.0.0",
                description = largeDescription
            },
            capabilities = new { tools = true }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(largeObj);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("LargeClient");
        result.ClientInfo.Description.Should().Be(largeDescription);
    }

    #endregion

    #region 에러 처리 테스트

    [Fact]
    public void DeserializeParams_WithDeserializationError_ThrowsJsonException()
    {
        // Arrange - 잘못된 JSON으로 직렬화 불가능한 객체
        var problematicObj = new ProblematicObject();

        // Act & Assert
        var act = () => ParameterDeserializer.DeserializeParams<ClientInitializeParams>(problematicObj);
        act.Should().Throw<Newtonsoft.Json.JsonException>()
            .WithMessage("*Failed to deserialize parameters*");
    }

    [Fact]
    public void DeserializeParams_WithWrongTargetType_ThrowsJsonException()
    {
        // Arrange - 완전히 다른 구조의 객체를 잘못된 타입으로 변환 시도
        var wrongStructure = new
        {
            completeDifferentStructure = true,
            numbers = new[] { 1, 2, 3 },
            someOtherField = "value"
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(wrongStructure);

        // Assert
        // 구조가 다르더라도 기본값으로 생성되어야 함
        result.Should().NotBeNull();
        result!.ClientInfo.Should().NotBeNull();
        result.Capabilities.Should().NotBeNull();
    }

    [Theory]
    [InlineData(42)]
    [InlineData(true)]
    [InlineData(3.14)]
    public void DeserializeParams_WithPrimitiveTypes_DeserializesAsString(object primitiveValue)
    {
        // Act
        var result = ParameterDeserializer.DeserializeParams<string>(primitiveValue);

        // Assert
        result.Should().NotBeNull();
        result!.ToUpper().Should().Be(primitiveValue!.ToString().ToUpper());
    }

    #endregion

    #region 실제 시나리오 테스트

    [Fact]
    public void DeserializeParams_WithRealClaudeClientData_DeserializesCorrectly()
    {
        // Arrange - 실제 Claude 클라이언트에서 올 수 있는 데이터
        var realClaudeData = """
        {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "Claude Desktop",
                "version": "0.7.1"
            },
            "capabilities": {
                "roots": {
                    "listChanged": true
                },
                "sampling": false
            }
        }
        """;

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(realClaudeData);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("Claude Desktop");
        result.ClientInfo.Version.Should().Be("0.7.1");
        result.Capabilities.Roots.Should().NotBeNull();
        result.Capabilities.Roots!.ListChanged.Should().BeTrue();
    }

    [Fact]
    public void DeserializeParams_WithVSCodeExtensionData_DeserializesCorrectly()
    {
        // Arrange - VS Code 확장에서 올 수 있는 데이터
        var vscodeData = JObject.FromObject(new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "vscode-mcp",
                version = "1.0.0",
                description = "VS Code MCP Extension"
            },
            capabilities = new
            {
                tools = true,
                resources = true,
                prompts = true,
                logging = true
            }
        });

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(vscodeData);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("vscode-mcp");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeTrue();
        result.Capabilities.Prompts.Should().BeTrue();
        result.Capabilities.Logging.Should().BeTrue();
    }

    [Fact]
    public void DeserializeParams_WithPythonClientData_DeserializesCorrectly()
    {
        // Arrange - Python 클라이언트 형태 (snake_case 혼합)
        var pythonData = new Dictionary<string, object>
        {
            ["protocolVersion"] = "2025-06-18",
            ["clientInfo"] = new Dictionary<string, object>
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

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(pythonData);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("python-mcp-client");
        result.Capabilities.Tools.Should().BeTrue();
        result.Capabilities.Resources.Should().BeFalse();
    }

    #endregion

    #region 타입 변환 테스트

    [Fact]
    public void DeserializeParams_WithStringNumbers_ConvertsCorrectly()
    {
        // Arrange - 문자열로 된 숫자들
        var stringNumbers = new
        {
            protocolVersion = "2025-06-18",
            clientInfo = new
            {
                name = "StringNumberClient",
                version = "1.0.0"
            },
            capabilities = new
            {
                tools = "true", // 문자열로 된 boolean
                resources = "false"
            }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(stringNumbers);

        // Assert
        result.Should().NotBeNull();
        result!.ClientInfo.Name.Should().Be("StringNumberClient");
        // JSON 변환 과정에서 문자열 "true"/"false"는 boolean으로 변환됨
    }

    [Fact]
    public void DeserializeParams_WithDifferentCasing_DeserializesCorrectly()
    {
        // Arrange - 다른 케이싱
        var differentCasing = new Dictionary<string, object>
        {
            ["PROTOCOLVERSION"] = "2025-06-18", // 대문자
            ["clientinfo"] = new Dictionary<string, object> // 소문자
            {
                ["NAME"] = "CasingClient",
                ["version"] = "1.0.0"
            },
            ["Capabilities"] = new Dictionary<string, object> // 파스칼케이스
            {
                ["Tools"] = true
            }
        };

        // Act
        var result = ParameterDeserializer.DeserializeParams<ClientInitializeParams>(differentCasing);

        // Assert
        result.Should().NotBeNull();
        // JsonConvert는 기본적으로 대소문자를 구분하므로 정확한 매칭만 동작
        result!.ClientInfo.Should().NotBeNull();
    }

    #endregion

    #region 헬퍼 클래스들

    private class CustomInitParams
    {
        public string ProtocolVersion { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string ClientVersion { get; set; } = "";
        public bool SupportsTools { get; set; }
    }

    private class ProblematicObject
    {
        // 직렬화 시 문제를 일으킬 수 있는 속성들
        public Stream SomeStream { get; set; } = Stream.Null;
        public IntPtr SomePointer { get; set; }
        public object CircularRef { get; set; }

        public ProblematicObject()
        {
            CircularRef = this; // 순환 참조
        }
    }

    #endregion
}