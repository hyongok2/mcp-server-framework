using System.Runtime.CompilerServices;
using System.Text.Json;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Interface;
using Micube.MCP.SDK.Streamable.Models;
using Micube.MCP.SDK.Streamable.Services;

namespace Micube.MCP.SDK.Streamable.Abstracts;

/// <summary>
/// Base class for streamable tool groups that extends BaseToolGroup
/// Provides streaming capabilities for MCP tools
/// </summary>
public abstract class BaseStreamableToolGroup : IStreamableMcpToolGroup
{
    public abstract string GroupName { get; }
    protected JsonElement? RawConfig { get; private set; }
    protected IMcpLogger Logger { get; }
    
    private readonly IToolMethodRegistry _methodRegistry;
    private readonly IStreamChunkFactory _chunkFactory;
    private readonly IMethodInvocationStrategyFactory _strategyFactory;
    private readonly IToolErrorHandler _errorHandler;

    protected BaseStreamableToolGroup(IMcpLogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize services
        _methodRegistry = new ToolMethodRegistry(GetType(), logger, GroupName);
        _chunkFactory = new StreamChunkFactory();
        _strategyFactory = new MethodInvocationStrategyFactory();
        _errorHandler = new ToolErrorHandler(_chunkFactory, logger);
    }

    public void Configure(JsonElement? config)
    {
        RawConfig = config;
        OnConfigure(config);
    }

    protected abstract void OnConfigure(JsonElement? config);

    /// <summary>
    /// Invokes a tool with streaming support
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> InvokeStreamAsync(
        string toolName,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var method = _methodRegistry.GetToolMethod(toolName);
        if (method == null)
        {
            yield return _errorHandler.HandleToolNotFound(toolName, GroupName);
            yield break;
        }

        // Log tool invocation
        Logger.LogDebug($"Invoking streamable tool: {GroupName}.{toolName}");

        // Start metadata chunk
        yield return _chunkFactory.CreateMetadataChunk(toolName, GroupName, parameters, 1);

        await foreach (var chunk in InvokeToolInternalAsync(toolName, method, parameters, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<StreamChunk> InvokeToolInternalAsync(
        string toolName,
        System.Reflection.MethodInfo method,
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get strategy - handle exception by creating error chunk
        var strategyResult = GetInvocationStrategy(method, toolName);
        if (strategyResult.ErrorChunk != null)
        {
            yield return strategyResult.ErrorChunk;
            yield break;
        }

        // Invoke strategy - handle exception by creating error chunk
        var chunksResult = await GetStrategyChunksAsync(strategyResult.Strategy!, method, parameters, cancellationToken, toolName);
        if (chunksResult.ErrorChunk != null)
        {
            yield return chunksResult.ErrorChunk;
            yield break;
        }

        await foreach (var chunk in chunksResult.Chunks!.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }
    }

    private (IMethodInvocationStrategy? Strategy, StreamChunk? ErrorChunk) GetInvocationStrategy(
        System.Reflection.MethodInfo method, string toolName)
    {
        try
        {
            var strategy = _strategyFactory.GetStrategy(method);
            return (strategy, null);
        }
        catch (NotSupportedException)
        {
            var errorChunk = _errorHandler.HandleUnsupportedMethod(toolName, method.ReturnType);
            return (null, errorChunk);
        }
    }

    private async Task<(IAsyncEnumerable<StreamChunk>? Chunks, StreamChunk? ErrorChunk)> GetStrategyChunksAsync(
        IMethodInvocationStrategy strategy,
        System.Reflection.MethodInfo method,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken,
        string toolName)
    {
        try
        {
            var chunks = strategy.InvokeAsync(method, this, parameters, cancellationToken, _chunkFactory);
            return (chunks, null);
        }
        catch (Exception ex)
        {
            var errorChunk = _errorHandler.HandleInvocationError(toolName, ex);
            return (null, errorChunk);
        }
    }
}