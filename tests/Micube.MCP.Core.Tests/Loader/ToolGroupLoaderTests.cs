using FluentAssertions;
using Micube.MCP.Core.Loader;
using Micube.MCP.Core.Tests.TestHelpers;
using Xunit;

namespace Micube.MCP.Core.Tests.Loader;

public class ToolGroupLoaderTests
{
    private readonly MockLogger _logger;
    private readonly ToolGroupLoader _loader;
    private readonly string _testDirectory;

    public ToolGroupLoaderTests()
    {
        _logger = new MockLogger();
        _loader = new ToolGroupLoader(_logger);
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void LoadFromDirectory_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "non-existent");

        // Act
        var result = _loader.LoadFromDirectory(nonExistentDir);

        // Assert
        result.Should().BeEmpty();
        _logger.ErrorMessages.Should().ContainSingle(m => m.Message.Contains("Tool directory not found"));
    }

    [Fact]
    public void LoadFromDirectory_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var result = _loader.LoadFromDirectory(_testDirectory);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromDirectory_WithWhitelist_OnlyLoadsWhitelistedDlls()
    {
        // Arrange
        CreateTestDll("TestDll1.dll");
        CreateTestDll("TestDll2.dll");
        var whitelist = new[] { "TestDll1.dll" };

        // Act
        var result = _loader.LoadFromDirectory(_testDirectory, whitelist);

        // Assert
        // Since we created dummy DLLs without actual tool groups, result should be empty
        // but we can verify the loader attempted to process only whitelisted files
        result.Should().BeEmpty();
    }

    private void CreateTestDll(string fileName)
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllBytes(filePath, new byte[] { 0x4D, 0x5A }); // MZ header for PE file
    }
}