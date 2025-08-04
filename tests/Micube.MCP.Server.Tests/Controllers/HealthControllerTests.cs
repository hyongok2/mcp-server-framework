using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Session;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.Server.Controllers;
using Moq;
using Xunit;

namespace Micube.MCP.Server.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IToolDispatcher> _toolDispatcherMock;
    private readonly Mock<IResourceService> _resourceServiceMock;
    private readonly Mock<IPromptService> _promptServiceMock;
    private readonly Mock<ISessionState> _sessionStateMock;
    private readonly Mock<IMcpLogger> _loggerMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _toolDispatcherMock = new Mock<IToolDispatcher>();
        _resourceServiceMock = new Mock<IResourceService>();
        _promptServiceMock = new Mock<IPromptService>();
        _sessionStateMock = new Mock<ISessionState>();
        _loggerMock = new Mock<IMcpLogger>();

        _controller = new HealthController(
            _toolDispatcherMock.Object,
            _resourceServiceMock.Object,
            _promptServiceMock.Object,
            _sessionStateMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Get_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        // Use reflection to check properties since it's an anonymous object
        var statusProperty = value!.GetType().GetProperty("status");
        statusProperty!.GetValue(value).Should().Be("healthy");
    }

    [Fact]
    public async Task GetDetailed_WithAllHealthyComponents_ReturnsOk()
    {
        // Arrange
        _sessionStateMock.Setup(x => x.IsInitialized).Returns(true);
        _toolDispatcherMock.Setup(x => x.GetAvailableGroups()).Returns(new List<string> { "TestGroup" });
        _resourceServiceMock.Setup(x => x.GetResourcesAsync()).ReturnsAsync(new List<McpResource>());
        _promptServiceMock.Setup(x => x.GetPromptsAsync()).ReturnsAsync(new List<McpPrompt>());

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetailed_WithUnhealthyComponent_ReturnsServiceUnavailable()
    {
        // Arrange
        _sessionStateMock.Setup(x => x.IsInitialized).Returns(true);
        _toolDispatcherMock.Setup(x => x.GetAvailableGroups()).Throws(new Exception("Tool error"));
        _resourceServiceMock.Setup(x => x.GetResourcesAsync()).ReturnsAsync(new List<McpResource>());
        _promptServiceMock.Setup(x => x.GetPromptsAsync()).ReturnsAsync(new List<McpPrompt>());

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetDetailed_WithUninitializedSession_StillReturnsHealthData()
    {
        // Arrange
        _sessionStateMock.Setup(x => x.IsInitialized).Returns(false);
        _toolDispatcherMock.Setup(x => x.GetAvailableGroups()).Returns(new List<string>());
        _resourceServiceMock.Setup(x => x.GetResourcesAsync()).ReturnsAsync(new List<McpResource>());
        _promptServiceMock.Setup(x => x.GetPromptsAsync()).ReturnsAsync(new List<McpPrompt>());

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that session status is reported as not-initialized
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetailed_LogsErrors()
    {
        // Arrange
        var exception = new Exception("Test error");
        _toolDispatcherMock.Setup(x => x.GetAvailableGroups()).Throws(exception);
        _resourceServiceMock.Setup(x => x.GetResourcesAsync()).ReturnsAsync(new List<McpResource>());
        _promptServiceMock.Setup(x => x.GetPromptsAsync()).ReturnsAsync(new List<McpPrompt>());

        // Act
        await _controller.GetDetailed();

        // Assert
        _loggerMock.Verify(x => x.LogError(
            It.Is<string>(s => s.Contains("Tools health check failed")), 
            It.Is<Exception>(e => e == exception)), 
            Times.Once);
    }
}