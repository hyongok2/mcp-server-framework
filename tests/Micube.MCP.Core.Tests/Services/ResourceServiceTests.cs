using FluentAssertions;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Tests.TestHelpers;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.Core.Tests.Services;

public class ResourceServiceTests : IDisposable
{
    private readonly MockLogger _logger;
    private readonly string _testDirectory;
    private readonly ResourceService _service;

    public ResourceServiceTests()
    {
        _logger = new MockLogger();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var options = new ResourceOptions
        {
            Directory = _testDirectory,
            MetadataFileName = ".mcp-resources.json",
            SupportedExtensions = new List<string> { ".md", ".txt", ".json" }
        };

        _service = new ResourceService(_logger, options);
    }

    [Fact]
    public async Task GetResourcesAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var resources = await _service.GetResourcesAsync();

        // Assert
        resources.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourcesAsync_WithSupportedFiles_ReturnsResources()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test.md"), "# Test");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "data.json"), "{}");

        // Act
        var resources = await _service.GetResourcesAsync();

        // Assert
        resources.Should().HaveCount(2);
        resources.Should().Contain(r => r.Name == "test.md");
        resources.Should().Contain(r => r.Name == "data.json");
    }

    [Fact]
    public async Task GetResourcesAsync_WithUnsupportedFiles_IgnoresUnsupportedFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test.md"), "# Test");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "image.png"), "binary data");

        // Act
        var resources = await _service.GetResourcesAsync();

        // Assert
        resources.Should().HaveCount(1);
        resources[0].Name.Should().Be("test.md");
    }

    [Fact]
    public async Task GetResourcesAsync_WithMetadata_UsesMetadataDescriptions()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test.md"), "# Test");
        
        var metadata = new Dictionary<string, string>
        {
            ["test.md"] = "Test markdown file"
        };
        
        var metadataJson = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".mcp-resources.json"), metadataJson);

        // Act
        var resources = await _service.GetResourcesAsync();

        // Assert
        resources.Should().HaveCount(1);
        resources[0].Description.Should().Be("Test markdown file");
    }

    [Fact]
    public async Task ReadResourceAsync_WithValidUri_ReturnsContent()
    {
        // Arrange
        var content = "# Test Content";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test.md"), content);
        await _service.RefreshResourcesAsync(); // Ensure resources are loaded

        // Act
        var result = await _service.ReadResourceAsync("file://test.md");

        // Assert
        result.Should().NotBeNull();
        result!.Text.Should().Be(content);
        result.MimeType.Should().Be("text/markdown");
    }

    [Fact]
    public async Task ReadResourceAsync_WithNonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _service.ReadResourceAsync("file://nonexistent.md");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadResourceAsync_WithSubdirectory_ReturnsContent()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        
        var content = "Subdirectory content";
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), content);

        // Act
        var result = await _service.ReadResourceAsync("file://subdir/nested.txt");

        // Assert
        result.Should().NotBeNull();
        result!.Text.Should().Be(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}