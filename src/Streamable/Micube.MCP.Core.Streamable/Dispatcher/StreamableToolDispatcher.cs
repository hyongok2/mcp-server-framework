using Micube.MCP.Core.MetaData;
using Micube.MCP.Core.Services.Tool;
using Micube.MCP.Core.Streamable.Services.Tool;
using Micube.MCP.SDK.Streamable.Models;
using System.Runtime.CompilerServices;

namespace Micube.MCP.Core.Streamable.Dispatcher;

/// <summary>
/// Service to coordinate and dispatch streamable tools
/// </summary>
public class StreamableToolDispatcher : IStreamableToolDispatcher
{
    private readonly IToolNameParser _toolNameParser;
    private readonly IToolGroupRegistry _toolGroupRegistry;
    private readonly IToolDispatcherErrorChunkFactory _errorChunkFactory;
    private readonly IToolExecutionCoordinator _executionCoordinator;

    public StreamableToolDispatcher(
        IToolNameParser toolNameParser,
        IToolGroupRegistry toolGroupRegistry,
        IToolDispatcherErrorChunkFactory errorChunkFactory,
        IToolExecutionCoordinator executionCoordinator)
    {
        _toolNameParser = toolNameParser;
        _toolGroupRegistry = toolGroupRegistry;
        _errorChunkFactory = errorChunkFactory;
        _executionCoordinator = executionCoordinator;
    }

    /// <summary>
    /// Invokes a tool with streaming support
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> InvokeStreamAsync(
        string fullToolName,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Parse tool name
        var parseResult = _toolNameParser.ParseToolName(fullToolName);
        if (!parseResult.IsValid)
        {
            yield return _errorChunkFactory.CreateToolNameParseErrorChunk(parseResult.ErrorMessage!);
            yield break;
        }

        // Find tool group
        var groupResult = _toolGroupRegistry.FindGroup(parseResult.GroupName);
        if (!groupResult.Found)
        {
            yield return _errorChunkFactory.CreateToolGroupNotFoundChunk(parseResult.GroupName);
            yield break;
        }

        // Execute tool
        await foreach (var chunk in _executionCoordinator.ExecuteToolStreamAsync(
            groupResult.Group!, parseResult.ToolName, parameters, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Gets the list of available tool groups
    /// </summary>
    public List<string> GetAvailableGroups()
    {
        return _toolGroupRegistry.GetAvailableGroups();
    }

    /// <summary>
    /// Gets metadata for a specific tool group
    /// </summary>
    public ToolGroupMetadata? GetGroupMetadata(string groupName)
    {
        return _toolGroupRegistry.GetGroupMetadata(groupName);
    }
}