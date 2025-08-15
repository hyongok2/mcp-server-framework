using System.Runtime.CompilerServices;
using System.Text.Json;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Micube.MCP.SDK.Streamable.Abstracts;
using Micube.MCP.SDK.Streamable.Models;

namespace SampleStreamableTools;

[McpToolGroup("SimpleStreamableDemo", "simple-streamable-demo.json", "Simple demonstration of streamable MCP tools")]
public class SimpleStreamableDemoToolGroup : BaseStreamableToolGroup
{
    public override string GroupName { get; } = "SimpleStreamableDemo";

    public SimpleStreamableDemoToolGroup(IMcpLogger logger) : base(logger)
    {
    }

    protected override void OnConfigure(JsonElement? config)
    {
        LogStreamOperation("Configure", "SimpleStreamableDemo tool group initialized");
    }

    private void LogStreamOperation(string operation, string message)
    {
        Logger.LogInfo($"[{operation}] {message}");
    }

    /// <summary>
    /// Regular (non-streaming) echo tool for compatibility
    /// </summary>
    [McpTool("simple_echo")]
    public Task<ToolCallResult> SimpleEchoAsync(Dictionary<string, object> parameters)
    {
        var input = parameters.ContainsKey("text") ? parameters["text"]?.ToString() : "Hello Streamable World!";
        Logger.LogInfo($"[SimpleEchoTool] Echo called with: {input}");
        
        var result = new { 
            echo = input, 
            timestamp = DateTime.UtcNow,
            toolType = "streamable-compatible"
        };
        
        return Task.FromResult(ToolCallResult.Success(JsonSerializer.Serialize(result)));
    }

    /// <summary>
    /// Stream counting numbers - simplified version
    /// </summary>
    [McpTool("simple_count")]
    public Task<ToolCallResult> SimpleCountAsync(Dictionary<string, object> parameters)
    {
        var maxCount = parameters.ContainsKey("count") && int.TryParse(parameters["count"]?.ToString(), out var c) ? c : 5;
        
        var results = new List<object>();
        for (int i = 1; i <= maxCount; i++)
        {
            results.Add(new { number = i, timestamp = DateTime.UtcNow });
        }
        
        Logger.LogInfo($"[SimpleCountTool] Generated {maxCount} numbers");
        return Task.FromResult(ToolCallResult.Success(JsonSerializer.Serialize(results)));
    }

    /// <summary>
    /// Streaming version of count - demonstrates real streaming
    /// </summary>
    [McpTool("stream_count")]
    public async IAsyncEnumerable<StreamChunk> StreamCountAsync(
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxCount = parameters.ContainsKey("count") && int.TryParse(parameters["count"]?.ToString(), out var c) ? c : 10;
        var delay = parameters.ContainsKey("delay") && int.TryParse(parameters["delay"]?.ToString(), out var d) ? d : 500;
        
        Logger.LogInfo($"[StreamCountTool] Starting to stream {maxCount} numbers with {delay}ms delay");

        for (int i = 1; i <= maxCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Emit progress chunk
            yield return new StreamChunk
            {
                Type = StreamChunkType.Progress,
                Content = $"Counting: {i}/{maxCount}",
                Progress = (double)i / maxCount,
                Timestamp = DateTime.UtcNow
            };

            // Emit content chunk
            yield return new StreamChunk
            {
                Type = StreamChunkType.Content,
                Content = JsonSerializer.Serialize(new { number = i, timestamp = DateTime.UtcNow }),
                Timestamp = DateTime.UtcNow
            };

            // Simulate processing delay
            await Task.Delay(delay, cancellationToken);
        }

        // Final completion chunk
        yield return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = $"Completed counting {maxCount} numbers",
            IsFinal = true,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["totalCount"] = maxCount,
                ["result"] = ToolCallResult.Success($"Successfully streamed {maxCount} numbers")
            }
        };
    }

    /// <summary>
    /// Streaming text generator - demonstrates streaming text chunks
    /// </summary>
    [McpTool("stream_text")]
    public async IAsyncEnumerable<StreamChunk> StreamTextAsync(
        Dictionary<string, object> parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = parameters.ContainsKey("text") ? parameters["text"]?.ToString() : "Hello from streaming world!";
        var words = text!.Split(' ');
        
        Logger.LogInfo($"[StreamTextTool] Streaming text with {words.Length} words");

        for (int i = 0; i < words.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Emit word as content chunk
            yield return new StreamChunk
            {
                Type = StreamChunkType.Content,
                Content = words[i] + (i < words.Length - 1 ? " " : ""),
                Progress = (double)(i + 1) / words.Length,
                Timestamp = DateTime.UtcNow
            };

            // Small delay to simulate streaming
            await Task.Delay(200, cancellationToken);
        }

        // Final chunk
        yield return new StreamChunk
        {
            Type = StreamChunkType.Complete,
            Content = "Text streaming completed",
            IsFinal = true,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["wordCount"] = words.Length,
                ["result"] = ToolCallResult.Success($"Streamed {words.Length} words")
            }
        };
    }
}