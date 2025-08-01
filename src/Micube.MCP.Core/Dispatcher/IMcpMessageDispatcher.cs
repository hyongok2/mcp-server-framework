using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Dispatcher;

public interface IMcpMessageDispatcher
{
    Task<object?> HandleAsync(McpMessage message); 
}