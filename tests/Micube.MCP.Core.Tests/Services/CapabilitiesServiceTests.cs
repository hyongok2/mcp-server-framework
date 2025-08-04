using FluentAssertions;
using Micube.MCP.Core.Models.Client;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Tests.TestHelpers;
using Micube.MCP.Core.Utils;
using Xunit;

namespace Micube.MCP.Core.Tests.Services;

public class CapabilitiesServiceTests
{
    private readonly CapabilitiesService _service;
    private readonly MockLogger _logger;

    public CapabilitiesServiceTests()
    {
        _logger = new MockLogger();
        _service = new CapabilitiesService(_logger);
    }

    [Fact]
    public void ValidateAndStore_WithValidParams_ReturnsSuccess()
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams();

        // Act
        var result = _service.ValidateAndStore(clientParams);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        var storedCapabilities = _service.GetClientCapabilities();
        storedCapabilities.Should().NotBeNull();
        storedCapabilities!.Tools.Should().BeTrue();
    }

    [Fact]
    public void ValidateAndStore_WithInvalidProtocolVersion_ReturnsSuccess()
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams();
        clientParams.ProtocolVersion = "1.0.0";

        // Act
        var result = _service.ValidateAndStore(clientParams);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateAndStore_WithMissingClientName_ReturnsSuccess(string? clientName)
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams(clientName!);

        // Act
        var result = _service.ValidateAndStore(clientParams);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(CapabilitiesConstants.Features.Tools, true)]
    [InlineData(CapabilitiesConstants.Features.Resources, true)]
    [InlineData(CapabilitiesConstants.Features.Prompts, true)]
    [InlineData(CapabilitiesConstants.Features.Sampling, false)]
    public void IsFeatureSupported_WithStoredCapabilities_ReturnsCorrectValue(string feature, bool expected)
    {
        // Arrange
        var clientParams = TestDataBuilder.CreateClientInitializeParams();
        _service.ValidateAndStore(clientParams);

        // Act
        var result = _service.IsFeatureSupported(feature);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetServerCapabilities_ReturnsCorrectCapabilities()
    {
        // Act
        var capabilities = _service.GetServerCapabilities();

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.Tools.Should().NotBeNull();
        capabilities.Resources.Should().NotBeNull();
        capabilities.Prompts.Should().NotBeNull();
        capabilities.Logging.Should().NotBeNull();
    }
}