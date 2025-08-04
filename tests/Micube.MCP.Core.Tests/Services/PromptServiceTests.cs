using FluentAssertions;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Services;
using Micube.MCP.Core.Tests.TestHelpers;
using System.Text.Json;
using Xunit;

namespace Micube.MCP.Core.Tests.Services;

public class PromptServiceTests : IDisposable
{
    private readonly MockLogger _logger;
    private readonly string _testDirectory;
    private readonly PromptService _service;

    public PromptServiceTests()
    {
        _logger = new MockLogger();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var options = new PromptOptions
        {
            Directory = _testDirectory
        };

        _service = new PromptService(_logger, options);
    }

    [Fact]
    public async Task GetPromptsAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var prompts = await _service.GetPromptsAsync();

        // Assert
        prompts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPromptsAsync_WithValidPromptDefinition_ReturnsPrompts()
    {
        // Arrange
        var promptDef = new PromptDefinition
        {
            Name = "test-prompt",
            Description = "Test prompt",
            TemplateFile = "test.txt",
            Arguments = new List<McpPromptArgument>
            {
                new McpPromptArgument { Name = "name", Required = true, Type = "string" }
            }
        };

        var json = JsonSerializer.Serialize(promptDef);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test-prompt.json"), json);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "test.txt"), "Hello {name}!");

        // Act
        var prompts = await _service.GetPromptsAsync();

        // Assert
        prompts.Should().HaveCount(1);
        prompts[0].Name.Should().Be("test-prompt");
        prompts[0].Description.Should().Be("Test prompt");
        prompts[0].Arguments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPromptAsync_WithValidPrompt_ReturnsRenderedContent()
    {
        // Arrange
        var promptDef = new PromptDefinition
        {
            Name = "greeting",
            Description = "Greeting prompt",
            TemplateFile = "greeting.txt"
        };

        var json = JsonSerializer.Serialize(promptDef);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "greeting.json"), json);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "greeting.txt"), "Hello {name}! Welcome to {place}.");

        // Ensure prompts are loaded
        await _service.GetPromptsAsync();

        var arguments = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["place"] = "Wonderland"
        };

        // Act
        var result = await _service.GetPromptAsync("greeting", arguments);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("Greeting prompt");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Text.Should().Be("Hello Alice! Welcome to Wonderland.");
    }

    [Fact]
    public async Task GetPromptAsync_WithNonExistentPrompt_ReturnsNull()
    {
        // Act
        var result = await _service.GetPromptAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromptAsync_WithMissingRequiredArgument_ReturnsNull()
    {
        // Arrange
        var promptDef = new PromptDefinition
        {
            Name = "required-arg",
            TemplateFile = "required.txt",
            Arguments = new List<McpPromptArgument>
            {
                new McpPromptArgument { Name = "required", Required = true }
            }
        };

        var json = JsonSerializer.Serialize(promptDef);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "required.json"), json);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "required.txt"), "Value: {required}");

        await _service.GetPromptsAsync();

        // Act
        var result = await _service.GetPromptAsync("required-arg", new Dictionary<string, object>());

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}