using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Handler;

public class HandlerErrorChunkFactory : IHandlerErrorChunkFactory
{
    public StreamChunk CreateParseErrorChunk(string errorMessage, int errorCode)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = errorMessage,
            IsFinal = true,
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["code"] = errorCode,
                ["message"] = "Invalid params format"
            }
        };
    }

    public StreamChunk CreateValidationErrorChunk(string errorMessage, int errorCode)
    {
        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = errorMessage,
            IsFinal = true,
            SequenceNumber = 1,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["code"] = errorCode,
                ["message"] = "Invalid params"
            }
        };
    }
}