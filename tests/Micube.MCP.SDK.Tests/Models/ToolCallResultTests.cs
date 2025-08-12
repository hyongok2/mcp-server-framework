using FluentAssertions;
using Micube.MCP.SDK.Models;
using Xunit;

namespace Micube.MCP.SDK.Tests.Models;

public class ToolCallResultTests
{
    [Fact]
    public void Success_WithSingleMessage_CreatesSuccessResult()
    {
        // Act
        var result = ToolCallResult.Success("Test message");

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().HaveCount(1);
        result.Content[0].Type.Should().Be("text");
        result.Content[0].Text.Should().Be("Test message");
    }

    [Fact]
    public void Success_WithMultipleMessages_CreatesSuccessResult()
    {
        // Act
        var result = ToolCallResult.Success("Message 1", "Message 2", "Message 3");

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().HaveCount(3);
        result.Content[0].Text.Should().Be("Message 1");
        result.Content[1].Text.Should().Be("Message 2");
        result.Content[2].Text.Should().Be("Message 3");
    }

    [Fact]
    public void Success_WithEmptyMessages_CreatesEmptyResult()
    {
        // Act
        var result = ToolCallResult.Success();

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void Fail_WithMessage_CreatesErrorResult()
    {
        // Act
        var result = ToolCallResult.Fail("Error message");

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().HaveCount(1);
        result.Content[0].Type.Should().Be("text");
        result.Content[0].Text.Should().Be("Error message");
    }

    [Fact]
    public void ToolContent_Constructor_SetsProperties()
    {
        // Act
        var content = new ToolContent("image", "Image data");

        // Assert
        content.Type.Should().Be("image");
        content.Text.Should().Be("Image data");
    }

    [Fact]
    public void ToolContent_DefaultConstructor_SetsDefaults()
    {
        // Act
        var content = new ToolContent();

        // Assert
        content.Type.Should().Be("text");
        content.Text.Should().BeNull();
    }
}