using FluentAssertions;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Exceptions;
using Xunit;

namespace Micube.MCP.Core.Tests.Validation;

public class MessageValidatorTests
{
    private readonly MessageValidator _validator = new();

    [Fact]
    public void Validate_WithValidMessage_ReturnsSuccess()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "test-id"
        };

        // Act
        var result = _validator.Validate(message);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorResponse.Should().BeNull();
    }

    [Fact]
    public void Validate_WithNullMessage_ReturnsError()
    {
        // Act
        var result = _validator.Validate(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorResponse.Should().NotBeNull();
        result.ErrorResponse!.Error.Code.Should().Be(McpErrorCodes.PARSE_ERROR);
    }

    [Fact]
    public void Validate_WithInvalidJsonRpcVersion_ReturnsError()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "1.0",
            Method = "initialize",
            Id = "test-id"
        };

        // Act
        var result = _validator.Validate(message);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorResponse!.Error.Code.Should().Be(McpErrorCodes.INVALID_REQUEST);
        result.ErrorResponse.Error.Message.Should().Contain("JSON-RPC version");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithMissingMethod_ReturnsError(string? method)
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = method,
            Id = "test-id"
        };

        // Act
        var result = _validator.Validate(message);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorResponse!.Error.Code.Should().Be(McpErrorCodes.INVALID_REQUEST);
        result.ErrorResponse.Error.Message.Should().Contain("method");
    }
}