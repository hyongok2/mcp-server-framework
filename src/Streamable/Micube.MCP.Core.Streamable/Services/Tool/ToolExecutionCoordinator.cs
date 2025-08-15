using Micube.MCP.Core.Streamable.Models;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;
using System.Runtime.CompilerServices;

namespace Micube.MCP.Core.Streamable.Services.Tool;

public class ToolExecutionCoordinator : IToolExecutionCoordinator
{
    private readonly IMcpLogger _logger;

    public ToolExecutionCoordinator(IMcpLogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<StreamChunk> ExecuteToolStreamAsync(
        LoadedStreamableToolGroup toolGroup,
        string toolName,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fullToolName = $"{toolGroup.GroupName}_{toolName}";
        _logger.LogDebug($"Executing streamable tool: {fullToolName}");

        var executionResult = await ExecuteToolInternalAsync(toolGroup, toolName, parameters, cancellationToken);
        
        if (executionResult.IsSuccess)
        {
            await foreach (var chunk in executionResult.Chunks!)
            {
                yield return chunk;
            }
        }
        else
        {
            yield return executionResult.ErrorChunk!;
        }
    }

    private async Task<ToolExecutionResult> ExecuteToolInternalAsync(
        LoadedStreamableToolGroup toolGroup,
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var fullToolName = $"{toolGroup.GroupName}_{toolName}";
        
        try
        {
            var chunks = toolGroup.GroupInstance.InvokeStreamAsync(toolName, parameters, cancellationToken);
            return ToolExecutionResult.Success(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing tool '{fullToolName}': {ex.Message}", ex);
            
            var errorChunk = new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Tool execution failed: {ex.Message}",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow
            };
            
            return ToolExecutionResult.Error(errorChunk);
        }
    }

    private class ToolExecutionResult
    {
        public bool IsSuccess { get; init; }
        public IAsyncEnumerable<StreamChunk>? Chunks { get; init; }
        public StreamChunk? ErrorChunk { get; init; }

        public static ToolExecutionResult Success(IAsyncEnumerable<StreamChunk> chunks) =>
            new() { IsSuccess = true, Chunks = chunks };

        public static ToolExecutionResult Error(StreamChunk errorChunk) =>
            new() { IsSuccess = false, ErrorChunk = errorChunk };
    }
}