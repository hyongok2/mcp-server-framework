using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Server.Streamable.Services;

public class SseFormatter : ISseFormatter
{
    public string FormatAsSSE(McpMessage request, StreamChunk chunk)
    {
        // Normalize progress to 0..1 if needed
        double? progress = chunk.Progress;
        if (progress.HasValue && progress.Value > 1.0)
        {
            progress = progress.Value / 100.0;
        }

        var payload = new
        {
            jsonrpc = "2.0",
            method = "mcp/streamChunk",
            @params = new
            {
                id = request.Id,
                sequence = chunk.SequenceNumber,
                chunk = new
                {
                    type = chunk.Type.ToString().ToLowerInvariant(),
                    content = chunk.Content,
                    isFinal = chunk.IsFinal,
                    timestamp = chunk.Timestamp.ToString("O"),
                    progress = progress,
                    metadata = chunk.Metadata
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var eventName = "mcp/streamChunk";
        var eventId = chunk.SequenceNumber.ToString();
        return $"event: {eventName}\n" +
               $"id: {eventId}\n" +
               $"data: {json}\n\n";
    }
}