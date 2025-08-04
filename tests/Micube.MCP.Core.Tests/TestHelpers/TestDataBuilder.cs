using Micube.MCP.Core.Models;
using Micube.MCP.Core.Models.Client;

namespace Micube.MCP.Core.Tests.TestHelpers;

public static class TestDataBuilder
{
    public static McpMessage CreateMessage(string method, object? id = null, object? parameters = null)
    {
        return new McpMessage
        {
            JsonRpc = "2.0",
            Method = method,
            Id = id ?? Guid.NewGuid().ToString(),
            Params = parameters
        };
    }

    public static ClientInitializeParams CreateClientInitializeParams(
        string clientName = "TestClient",
        string version = "1.0.0",
        ClientCapabilities? capabilities = null)
    {
        return new ClientInitializeParams
        {
            ProtocolVersion = "2025-06-18",
            ClientInfo = new ClientInfo
            {
                Name = clientName,
                Version = version,
                Description = "Test client"
            },
            Capabilities = capabilities ?? new ClientCapabilities
            {
                Tools = true,
                Resources = true,
                Prompts = true
            }
        };
    }

    public static McpToolCallRequest CreateToolCallRequest(string toolName, Dictionary<string, object>? arguments = null)
    {
        return new McpToolCallRequest
        {
            Name = toolName,
            Arguments = arguments ?? new Dictionary<string, object>()
        };
    }

    public static McpResourceReadRequest CreateResourceReadRequest(string uri)
    {
        return new McpResourceReadRequest
        {
            Uri = uri
        };
    }

    public static McpPromptGetRequest CreatePromptGetRequest(string name, Dictionary<string, object>? arguments = null)
    {
        return new McpPromptGetRequest
        {
            Name = name,
            Arguments = arguments
        };
    }
}