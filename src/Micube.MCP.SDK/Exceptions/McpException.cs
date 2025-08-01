using System;

namespace Micube.MCP.SDK.Exceptions;

public class McpException : Exception
{
    public McpException(string message) : base(message) { }
    public McpException(string message, Exception innerException) : base(message, innerException) { }
}
