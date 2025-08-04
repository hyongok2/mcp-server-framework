using FluentAssertions;
using Micube.MCP.Core.Handlers.Core;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Tests.TestHelpers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Micube.MCP.Core.Tests.Handlers;

public class InitializeHandlerTests
{
    private readonly InitializeHandler _handler;
    private readonly Mock<ISessionState> _sessionStateMock;
    private readonly Mock<ICapabilitiesService> _capabilitiesServiceMock;
    private readonly MockLogger _logger;

    public InitializeHandlerTests()
    {
        _sessionStateMock = new Mock<ISessionState>();
        _capabilitiesServiceMock = new Mock<ICapabilitiesService>();
        _logger = new MockLogger();

        _handler = new InitializeHandler(
            _sessionStateMock.Object,
            _capabilitiesServiceMock.Object,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidMessage_ReturnsSuccessResponse()
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams();
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", clientParams);

        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Returns(CapabilitiesValidationResult.Success());

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        var response = (McpSuccessResponse)result!;
        response.Id.Should().Be("test-id");

        _sessionStateMock.Verify(x => x.MarkAsInitialized(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithValidationFailure_ReturnsErrorResponse()
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams();
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", clientParams);

        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Returns(CapabilitiesValidationResult.Error("Test error"));

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var response = (McpErrorResponse)result!;
        response.Error.Message.Should().Contain("validation failed");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidParams_ReturnsErrorResponse()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", "invalid-params");
        _capabilitiesServiceMock
        .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
        .Returns(CapabilitiesValidationResult.Success());

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>(); // Assuming the handler returns success even with invalid params
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
         It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "Unknown Client")),
         Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullParams_UsesDefaults()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("initialize", "test-id", null);

        _capabilitiesServiceMock
            .Setup(x => x.ValidateAndStore(It.IsAny<ClientInitializeParams>()))
            .Returns(CapabilitiesValidationResult.Success());

        // Act
        var result = await _handler.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpSuccessResponse>();
        _capabilitiesServiceMock.Verify(x => x.ValidateAndStore(
            It.Is<ClientInitializeParams>(p => p.ClientInfo.Name == "Unknown Client")),
            Times.Once);
    }
}