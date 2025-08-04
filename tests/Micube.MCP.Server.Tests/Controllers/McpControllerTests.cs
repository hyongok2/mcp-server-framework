using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Controllers;
using Micube.MCP.Server.Options;
using Moq;
using Xunit;

namespace Micube.MCP.Server.Tests.Controllers;

public class McpControllerTests
{
    private readonly Mock<IMcpMessageDispatcher> _dispatcherMock;
    private readonly Mock<IMcpLogger> _loggerMock;
    private readonly McpController _controller;
    private readonly FeatureOptions _featureOptions;

    public McpControllerTests()
    {
        _dispatcherMock = new Mock<IMcpMessageDispatcher>();
        _loggerMock = new Mock<IMcpLogger>();
        _featureOptions = new FeatureOptions { EnableHttp = true };
        
        var optionsMock = new Mock<IOptions<FeatureOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_featureOptions);

        _controller = new McpController(_dispatcherMock.Object, _loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task Post_WithValidMessage_ReturnsOkResult()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "test-id"
        };

        var expectedResponse = new McpSuccessResponse
        {
            Id = "test-id",
            Result = new { status = "success" }
        };

        _dispatcherMock.Setup(x => x.HandleAsync(It.IsAny<McpMessage>()))
                      .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Post(message);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Post_WithNullMessage_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Post(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithNotificationMessage_ReturnsNoContent()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "notifications/initialized",
            Id = null // Notification has no ID
        };

        _dispatcherMock.Setup(x => x.HandleAsync(It.IsAny<McpMessage>()))
                      .ReturnsAsync((object?)null); // Notifications return null

        // Act
        var result = await _controller.Post(message);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Post_WithHttpDisabled_ReturnsServiceUnavailable()
    {
        // Arrange
        _featureOptions.EnableHttp = false;
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "test-id"
        };

        // Act
        var result = await _controller.Post(message);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task Post_LogsRequestAndResponse()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "ping",
            Id = "test-id"
        };

        var response = new McpSuccessResponse { Id = "test-id" };
        _dispatcherMock.Setup(x => x.HandleAsync(It.IsAny<McpMessage>()))
                      .ReturnsAsync(response);

        // Act
        await _controller.Post(message);

        // Assert
        _loggerMock.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("[HTTP] Received message"))), Times.Once);
        _loggerMock.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("[HTTP] Response"))), Times.Once);
    }
}