using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Micube.MCP.Core.Models;
using Micube.MCP.Server;
using Newtonsoft.Json;
using System.Text;
using Xunit;

namespace Micube.MCP.Server.Tests.Integration;

public class ServerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ServerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Override any services for testing if needed
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task DetailedHealthCheck_ReturnsComponentStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/detailed");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("components");
        content.Should().Contain("session");
        content.Should().Contain("tools");
        content.Should().Contain("resources");
        content.Should().Contain("prompts");
    }

    [Fact]
    public async Task McpEndpoint_WithPingMessage_ReturnsSuccessResponse()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "ping",
            Id = "test-ping"
        };

        var json = JsonConvert.SerializeObject(message);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var mcpResponse = JsonConvert.DeserializeObject<McpSuccessResponse>(responseContent);
        mcpResponse.Should().NotBeNull();
        mcpResponse!.Id.Should().Be("test-ping");
    }

    [Fact]
    public async Task McpEndpoint_WithInitializeMessage_ReturnsServerInfo()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "test-init",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "TestClient", version = "1.0.0" },
                capabilities = new { tools = true, resources = true, prompts = true }
            }
        };

        var json = JsonConvert.SerializeObject(message);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        responseContent.Should().Contain("serverInfo");
        responseContent.Should().Contain("capabilities");
        responseContent.Should().Contain("Micube MCP Server Framework");
    }

    [Fact]
    public async Task McpEndpoint_WithInvalidMessage_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task McpEndpoint_WithToolsListMessage_ReturnsToolList()
    {
        // Arrange - First initialize
        await InitializeServer();

        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "tools/list",
            Id = "test-tools-list"
        };

        var json = JsonConvert.SerializeObject(message);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        responseContent.Should().Contain("tools");
    }

    private async Task InitializeServer()
    {
        var initMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = "init",
            Params = new
            {
                protocolVersion = "2025-06-18",
                clientInfo = new { name = "TestClient", version = "1.0.0" },
                capabilities = new { tools = true, resources = true, prompts = true }
            }
        };

        var json = JsonConvert.SerializeObject(initMessage);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await _client.PostAsync("/mcp", content);
    }
}