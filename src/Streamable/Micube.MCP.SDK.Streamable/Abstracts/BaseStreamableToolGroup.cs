using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Interface;
using Micube.MCP.SDK.Streamable.Models;

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
    private readonly Dictionary<string, MethodInfo> _toolMethodCache;

    protected BaseStreamableToolGroup(IMcpLogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _toolMethodCache = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
            .ToDictionary(
                m => m.GetCustomAttribute<McpToolAttribute>()!.Name,
                m => m,
                StringComparer.OrdinalIgnoreCase);

        Logger.LogDebug($"Tool group '{GroupName}' initialized with {_toolMethodCache.Count} tools.");
        foreach (var tool in _toolMethodCache)
        {
            Logger.LogDebug($"Tool registered: {tool.Key}, Signature: {tool.Value}");
        }
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
        if (!_toolMethodCache.TryGetValue(toolName, out var method))
        {
            yield return new StreamChunk
            {
                Type = StreamChunkType.Error,
                Content = $"Tool '{toolName}' not found in group '{GroupName}'",
                IsFinal = true,
                SequenceNumber = 1,
                Timestamp = DateTime.UtcNow
            };
            yield break;
        }

        // Log tool invocation
        Logger.LogDebug($"Invoking streamable tool: {GroupName}.{toolName}");

        // Start metadata chunk
        yield return new StreamChunk
        {
            Type = StreamChunkType.Metadata,
            Content = $"Starting tool: {toolName}",
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["tool"] = toolName,
                ["group"] = GroupName,
                ["parameters"] = parameters
            }
        };

        // Check if method returns IAsyncEnumerable<StreamChunk> (streaming)
        var returnType = method.ReturnType;
        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
            returnType.GetGenericArguments()[0] == typeof(StreamChunk))
        {
            // Streaming method
            var result = method.Invoke(this, new object[] { parameters, cancellationToken });
            if (result is IAsyncEnumerable<StreamChunk> stream)
            {
                var sequenceNumber = 2;
                await foreach (var chunk in stream.WithCancellation(cancellationToken))
                {
                    chunk.SequenceNumber = sequenceNumber++;
                    yield return chunk;
                }
            }
        }
        else if (returnType == typeof(Task<ToolCallResult>))
        {
            // Non-streaming method - wrap result in stream chunks
            var task = (Task<ToolCallResult>)method.Invoke(this, new object[] { parameters })!;
            var result = await task;

            // Progress chunk
            yield return new StreamChunk
            {
                Type = StreamChunkType.Progress,
                Content = "Processing...",
                SequenceNumber = 2,
                Progress = 0.5,
                Timestamp = DateTime.UtcNow
            };

            // Complete chunk with result
            yield return new StreamChunk
            {
                Type = StreamChunkType.Complete,
                Content = "Tool execution completed",
                IsFinal = true,
                SequenceNumber = 3,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["result"] = result
                }
            };
        }
        else
        {
            throw new NotSupportedException($"Tool method '{toolName}' has unsupported return type: {returnType}");
        }
    }
}