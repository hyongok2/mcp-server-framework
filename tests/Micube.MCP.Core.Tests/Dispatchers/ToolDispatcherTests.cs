using FluentAssertions;
using Micube.MCP.Core.Dispatcher;
using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Tests.TestHelpers;
using Xunit;

namespace Micube.MCP.Core.Tests.Dispatchers;

public class ToolDispatcherTests
{
    private readonly ToolDispatcher _dispatcher;
    private readonly MockLogger _logger;

    public ToolDispatcherTests()
    {
        _logger = new MockLogger();
        var testGroup = new TestToolGroup(_logger);
        var loadedGroups = new List<LoadedToolGroup>
        {
            new LoadedToolGroup
            {
                GroupName = "TestGroup",
                GroupInstance = testGroup,
                Description = "Test group",
                ManifestPath = "test.json",
                Metadata = new ToolGroupMetadata
                {
                    GroupName = "TestGroup",
                    Tools = new List<ToolDescriptor>
                    {
                        new ToolDescriptor { Name = "TestTool", Description = "Test tool" }
                    }
                }
            }
        };

        _dispatcher = new ToolDispatcher(loadedGroups, _logger);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTool_ReturnsSuccess()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "message", "test" } };

        // Act
        var result = await _dispatcher.InvokeAsync("TestGroup_TestTool", parameters);

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().HaveCount(1);
        result.Content[0].Text.Should().Contain("Test: test");
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidToolName_ReturnsError()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _dispatcher.InvokeAsync("InvalidTool", parameters);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Invalid tool name format");
    }

    [Fact]
    public async Task InvokeAsync_WithNonExistentGroup_ReturnsError()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act
        var result = await _dispatcher.InvokeAsync("NonExistent_Tool", parameters);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("ToolGroup 'NonExistent' not found");
    }

    [Fact]
    public void GetAvailableGroups_ReturnsCorrectGroups()
    {
        // Act
        var groups = _dispatcher.GetAvailableGroups();

        // Assert
        groups.Should().HaveCount(1);
        groups[0].Should().Be("TestGroup");
    }

    [Fact]
    public void GetGroupMetadata_WithValidGroup_ReturnsMetadata()
    {
        // Act
        var metadata = _dispatcher.GetGroupMetadata("TestGroup");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.GroupName.Should().Be("TestGroup");
        metadata.Tools.Should().HaveCount(1);
    }

    [Fact]
    public void GetGroupMetadata_WithInvalidGroup_ReturnsNull()
    {
        // Act
        var metadata = _dispatcher.GetGroupMetadata("NonExistent");

        // Assert
        metadata.Should().BeNull();
    }
}
