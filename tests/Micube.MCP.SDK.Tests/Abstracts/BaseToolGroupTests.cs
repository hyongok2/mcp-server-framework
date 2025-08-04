using FluentAssertions;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Moq;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.SDK.Tests.Abstracts;

public class BaseToolGroupTests
{
    private readonly Mock<IMcpLogger> _loggerMock;
    private readonly TestableToolGroup _toolGroup;

    public BaseToolGroupTests()
    {
        _loggerMock = new Mock<IMcpLogger>();
        _toolGroup = new TestableToolGroup(_loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTool_ReturnsSuccess()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "input", "test" } };

        // Act
        var result = await _toolGroup.InvokeAsync("StringTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().HaveCount(1);
        result.Content[0].Text.Should().Be("test");
    }

    [Fact]
    public async Task InvokeAsync_WithIntReturnTool_ReturnsFormattedResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("IntTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Be("42");
    }

    [Fact]
    public async Task InvokeAsync_WithBoolReturnTool_ReturnsFormattedResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("BoolTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Be("True");
    }

    [Fact]
    public async Task InvokeAsync_WithObjectReturnTool_ReturnsJsonResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("ObjectTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("\"name\":\"test\"");
        result.Content[0].Text.Should().Contain("\"value\":123");
    }

    [Fact]
    public async Task InvokeAsync_WithVoidTool_ReturnsSuccessMessage()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("VoidTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("completed successfully");
    }

    [Fact]
    public async Task InvokeAsync_WithToolCallResultTool_ReturnsDirectResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("ToolCallResultTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Be("Direct result");
    }

    [Fact]
    public async Task InvokeAsync_WithThrowingTool_ReturnsErrorResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("ThrowingTool", parameters);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Tool execution failed");
        result.Content[0].Text.Should().Contain("Test exception");
    }

    [Fact]
    public async Task InvokeAsync_WithNonExistentTool_ThrowsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<Micube.MCP.SDK.Exceptions.McpException>(
            () => _toolGroup.InvokeAsync("NonExistentTool", parameters));
    }

    [Fact]
    public async Task InvokeAsync_WithNullReturnTool_ReturnsNullResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _toolGroup.InvokeAsync("NullTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Be("null");
    }

    [Fact]
    public void Configure_WithValidConfig_CallsOnConfigure()
    {
        // Arrange
        var config = JsonDocument.Parse("""{"test": "value"}""").RootElement;

        // Act
        _toolGroup.Configure(config);

        // Assert
        _toolGroup.ConfigureCalled.Should().BeTrue();
        _toolGroup.LastConfig.Should().NotBeNull();
    }

    [Fact]
    public void Configure_WithNullConfig_CallsOnConfigureWithNull()
    {
        // Act
        _toolGroup.Configure(null);

        // Assert
        _toolGroup.ConfigureCalled.Should().BeTrue();
        _toolGroup.LastConfig.Should().BeNull();
    }
}