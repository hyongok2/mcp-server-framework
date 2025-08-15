using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public class ToolErrorHandler : IToolErrorHandler
{
    private readonly IStreamChunkFactory _chunkFactory;
    private readonly IMcpLogger _logger;

    public ToolErrorHandler(IStreamChunkFactory chunkFactory, IMcpLogger logger)
    {
        _chunkFactory = chunkFactory;
        _logger = logger;
    }

    public StreamChunk HandleToolNotFound(string toolName, string groupName)
    {
        var errorMessage = $"Tool '{toolName}' not found in group '{groupName}'";
        _logger.LogError(errorMessage);
        return _chunkFactory.CreateErrorChunk(errorMessage, 1);
    }

    public StreamChunk HandleUnsupportedMethod(string toolName, Type returnType)
    {
        var errorMessage = $"Tool method '{toolName}' has unsupported return type: {returnType}";
        _logger.LogError(errorMessage);
        return _chunkFactory.CreateErrorChunk(errorMessage, 1);
    }

    public StreamChunk HandleInvocationError(string toolName, Exception exception)
    {
        var errorMessage = $"Error invoking tool '{toolName}': {exception.Message}";
        _logger.LogError(errorMessage, exception);
        return _chunkFactory.CreateErrorChunk(errorMessage, 1);
    }
}