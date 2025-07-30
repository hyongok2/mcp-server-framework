using System;

namespace Micube.MCP.SDK.Exceptions;

public class McpToolNotFoundException : Exception
{
    public McpToolNotFoundException(string message) : base(message)
    {
    }

    public McpToolNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
