using FluentAssertions;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Session;
using Micube.MCP.Core.Tests.TestHelpers;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Exceptions;
using Moq;
using Xunit;

namespace Micube.MCP.Core.Tests.Dispatchers;

public class McpMessageDispatcherTests
{
    private readonly Mock<IMessageValidator> _validatorMock;
    private readonly Mock<ISessionState> _sessionStateMock;
    private readonly MockLogger _logger;
    private readonly McpMessageDispatcher _dispatcher;
    private readonly Mock<IMethodHandler> _handlerMock;

    public McpMessageDispatcherTests()
    {
        _validatorMock = new Mock<IMessageValidator>();
        _sessionStateMock = new Mock<ISessionState>();
        _logger = new MockLogger();
        _handlerMock = new Mock<IMethodHandler>();

        _handlerMock.Setup(h => h.MethodName).Returns("test-method");
        _handlerMock.Setup(h => h.RequiresInitialization).Returns(true);

        var handlers = new[] { _handlerMock.Object };

        _dispatcher = new McpMessageDispatcher(
            _validatorMock.Object,
            _sessionStateMock.Object,
            handlers,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ReturnsError()
    {
        // Act
        var result = await _dispatcher.HandleAsync(null!);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Code.Should().Be(McpErrorCodes.INVALID_REQUEST);
    }

    [Fact]
    public async Task HandleAsync_WithValidationFailure_ReturnsValidationError()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("test-method");
        var validationError = new McpErrorResponse
        {
            Error = new McpError { Code = McpErrorCodes.INVALID_REQUEST, Message = "Validation failed" }
        };

        _validatorMock.Setup(v => v.Validate(message))
                     .Returns(ValidationResult.Error(McpErrorCodes.INVALID_REQUEST, "Validation failed"));

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Message.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task HandleAsync_WithUnknownMethod_ReturnsMethodNotFound()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("unknown-method");
        _validatorMock.Setup(v => v.Validate(message)).Returns(ValidationResult.Success());

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Code.Should().Be(McpErrorCodes.METHOD_NOT_FOUND);
    }

    [Fact]
    public async Task HandleAsync_WithUninitializedSession_ReturnsError()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("test-method");
        _validatorMock.Setup(v => v.Validate(message)).Returns(ValidationResult.Success());
        _sessionStateMock.Setup(s => s.IsInitialized).Returns(false);

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Message.Should().Contain("not initialized");
    }

    [Fact]
    public async Task HandleAsync_WithValidMessage_CallsHandler()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("test-method");
        var expectedResponse = new McpSuccessResponse { Id = message.Id };

        _validatorMock.Setup(v => v.Validate(message)).Returns(ValidationResult.Success());
        _sessionStateMock.Setup(s => s.IsInitialized).Returns(true);
        _handlerMock.Setup(h => h.HandleAsync(message)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().Be(expectedResponse);
        _handlerMock.Verify(h => h.HandleAsync(message), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithHandlerException_ReturnsInternalError()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("test-method");
        _validatorMock.Setup(v => v.Validate(message)).Returns(ValidationResult.Success());
        _sessionStateMock.Setup(s => s.IsInitialized).Returns(true);
        _handlerMock.Setup(h => h.HandleAsync(message)).ThrowsAsync(new Exception("Handler error"));

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().BeOfType<McpErrorResponse>();
        var errorResponse = (McpErrorResponse)result!;
        errorResponse.Error.Code.Should().Be(McpErrorCodes.INTERNAL_ERROR);
    }

    [Fact]
    public async Task HandleAsync_WithNotification_ReturnsNull()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage("test-method");
        _validatorMock.Setup(v => v.Validate(message)).Returns(ValidationResult.Success());
        _sessionStateMock.Setup(s => s.IsInitialized).Returns(true);
        _handlerMock.Setup(h => h.HandleAsync(message)).ReturnsAsync((object?)null);

        // Act
        var result = await _dispatcher.HandleAsync(message);

        // Assert
        result.Should().BeNull();
    }
}