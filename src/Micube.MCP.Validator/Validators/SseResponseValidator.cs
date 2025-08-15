using System.Net.Http;
using System.Text;
using System.Text.Json;
using Micube.MCP.SDK.Streamable.Models;
using Micube.MCP.Validator.Models;

namespace Micube.MCP.Validator.Validators;

/// <summary>
/// Validates SSE (Server-Sent Events) responses from streaming endpoints
/// </summary>
public class SseResponseValidator : IValidator
{
    private readonly HttpClient _httpClient;
    private readonly string? _serverUrl;

    public SseResponseValidator(string? serverUrl = null)
    {
        _serverUrl = serverUrl ?? "http://localhost:5556";
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string Name => "SSE Response Validator";

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var report = new ValidationReport { Context = context };
        var startTime = DateTime.UtcNow;

        if (context.Level < ValidationLevel.Full)
        {
            report.AddInfo("SSE", "SSE001", 
                "SSE validation skipped (ValidationLevel < Full)");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        // Check if server is running
        if (!await IsServerRunningAsync(report))
        {
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        // Test SSE endpoint
        await TestSseEndpointAsync(report);

        // Test streaming response format
        await TestStreamingResponseAsync(report);

        report.Duration = DateTime.UtcNow - startTime;
        return report;
    }

    private async Task<bool> IsServerRunningAsync(ValidationReport report)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/health");
            if (response.IsSuccessStatusCode)
            {
                report.AddSuccess("SSE", "SSE010", 
                    $"Server is running at {_serverUrl}");
                return true;
            }
            else
            {
                report.AddError("SSE", "SSE011", 
                    $"Server returned {response.StatusCode} from health endpoint");
                return false;
            }
        }
        catch (Exception ex)
        {
            report.AddError("SSE", "SSE012", 
                $"Cannot connect to server at {_serverUrl}: {ex.Message}");
            return false;
        }
    }

    private async Task TestSseEndpointAsync(ValidationReport report)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_serverUrl}/mcp");
            request.Headers.Add("Accept", "text/event-stream");
            
            var jsonContent = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "ping",
                @params = new { }
            });

            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                report.AddError("SSE", "SSE020", 
                    $"SSE endpoint returned {response.StatusCode}");
                return;
            }

            // Check Content-Type
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != "text/event-stream")
            {
                report.AddError("SSE", "SSE021", 
                    $"Invalid Content-Type: {contentType}, expected 'text/event-stream'");
            }
            else
            {
                report.AddSuccess("SSE", "SSE022", 
                    "Valid SSE Content-Type header");
            }

            // Check other SSE headers
            if (response.Headers.CacheControl?.NoCache != true)
            {
                report.AddWarning("SSE", "SSE023", 
                    "Missing 'Cache-Control: no-cache' header");
            }

            if (!response.Headers.Contains("X-Accel-Buffering"))
            {
                report.AddInfo("SSE", "SSE024", 
                    "Missing 'X-Accel-Buffering: no' header (optional but recommended)");
            }

            report.AddSuccess("SSE", "SSE025", 
                "SSE endpoint is accessible");
        }
        catch (Exception ex)
        {
            report.AddError("SSE", "SSE029", 
                $"Failed to test SSE endpoint: {ex.Message}");
        }
    }

    private async Task TestStreamingResponseAsync(ValidationReport report)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_serverUrl}/mcp");
            
            // Test with a sample streaming tool call
            var jsonContent = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = "SampleStreamableTools_StreamData",
                    arguments = new { count = 3 }
                }
            });

            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                report.AddWarning("SSE", "SSE030", 
                    $"Streaming test returned {response.StatusCode} (tool might not exist)");
                return;
            }

            // Read SSE stream
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var chunks = new List<StreamChunk>();
            var lines = new List<string>();
            var eventCount = 0;

            while (!reader.EndOfStream && eventCount < 10) // Limit for testing
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                lines.Add(line);

                if (line.StartsWith("data: "))
                {
                    eventCount++;
                    var data = line.Substring(6);
                    
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        report.AddWarning("SSE", "SSE031", 
                            $"Empty data field at event {eventCount}");
                        continue;
                    }

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                        if (chunk != null)
                        {
                            chunks.Add(chunk);
                            ValidateSseChunk(chunk, eventCount, report);
                        }
                    }
                    catch (JsonException ex)
                    {
                        report.AddError("SSE", "SSE032", 
                            $"Invalid JSON in SSE data at event {eventCount}: {ex.Message}");
                    }
                }
                else if (line.StartsWith(":"))
                {
                    // Comment line (heartbeat)
                    report.AddInfo("SSE", "SSE033", 
                        $"Received SSE comment: {line}");
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    report.AddWarning("SSE", "SSE034", 
                        $"Unexpected SSE line format: {line}");
                }
            }

            if (eventCount == 0)
            {
                report.AddError("SSE", "SSE035", 
                    "No SSE data events received");
            }
            else
            {
                report.AddSuccess("SSE", "SSE036", 
                    $"Received {eventCount} SSE data event(s)");
            }

            // Validate chunk sequence
            if (chunks.Any())
            {
                ValidateChunkSequence(chunks, report);
            }
        }
        catch (TaskCanceledException)
        {
            report.AddWarning("SSE", "SSE039", 
                "Streaming test timed out (might be normal for long streams)");
        }
        catch (Exception ex)
        {
            report.AddError("SSE", "SSE040", 
                $"Failed to test streaming response: {ex.Message}");
        }
    }

    private void ValidateSseChunk(StreamChunk chunk, int eventNumber, ValidationReport report)
    {
        // Type validation
        if (!Enum.IsDefined(typeof(StreamChunkType), chunk.Type))
        {
            report.AddError("SSE", "SSE050", 
                $"Invalid chunk type '{chunk.Type}' at event {eventNumber}");
        }

        // Content validation
        if (chunk.Type == StreamChunkType.Content && string.IsNullOrEmpty(chunk.Content))
        {
            report.AddWarning("SSE", "SSE051", 
                $"Empty content in Content-type chunk at event {eventNumber}");
        }

        // Progress validation
        if (chunk.Type == StreamChunkType.Progress)
        {
            if (!chunk.Progress.HasValue)
            {
                report.AddWarning("SSE", "SSE052", 
                    $"Progress chunk without progress value at event {eventNumber}");
            }
            else if (chunk.Progress < 0 || chunk.Progress > 1)
            {
                report.AddError("SSE", "SSE053", 
                    $"Invalid progress value {chunk.Progress} at event {eventNumber}");
            }
        }

        // Final chunk validation
        if (chunk.IsFinal && chunk.Type != StreamChunkType.Complete && chunk.Type != StreamChunkType.Error)
        {
            report.AddWarning("SSE", "SSE054", 
                $"IsFinal=true but type is {chunk.Type} (expected Complete or Error) at event {eventNumber}");
        }
    }

    private void ValidateChunkSequence(List<StreamChunk> chunks, ValidationReport report)
    {
        // Check sequence numbers if present
        var hasSequenceNumbers = chunks.Any(c => c.SequenceNumber > 0);
        if (hasSequenceNumbers)
        {
            var expectedSequence = 1;
            foreach (var chunk in chunks)
            {
                if (chunk.SequenceNumber != expectedSequence)
                {
                    report.AddWarning("SSE", "SSE060", 
                        $"Sequence number mismatch: expected {expectedSequence}, got {chunk.SequenceNumber}");
                }
                expectedSequence++;
            }
        }

        // Check for final chunk
        var finalChunks = chunks.Where(c => c.IsFinal).ToList();
        if (finalChunks.Count == 0)
        {
            report.AddWarning("SSE", "SSE061", 
                "No final chunk in stream");
        }
        else if (finalChunks.Count > 1)
        {
            report.AddError("SSE", "SSE062", 
                $"Multiple final chunks found ({finalChunks.Count})");
        }
        else
        {
            var lastChunk = chunks.Last();
            if (!lastChunk.IsFinal)
            {
                report.AddWarning("SSE", "SSE063", 
                    "Final chunk is not the last chunk in sequence");
            }
        }

        // Check timestamps
        DateTime? previousTimestamp = null;
        foreach (var chunk in chunks)
        {
            if (previousTimestamp.HasValue && chunk.Timestamp < previousTimestamp)
            {
                report.AddWarning("SSE", "SSE064", 
                    "Timestamp goes backward in chunk sequence");
            }
            previousTimestamp = chunk.Timestamp;
        }

        report.AddSuccess("SSE", "SSE065", 
            "Chunk sequence validation completed");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}