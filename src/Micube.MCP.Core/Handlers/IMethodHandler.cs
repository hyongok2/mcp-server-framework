using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Handlers;

public interface IMethodHandler
{
    string MethodName { get; }
    bool RequiresInitialization { get; }
    Task<object?> HandleAsync(McpMessage message);
}