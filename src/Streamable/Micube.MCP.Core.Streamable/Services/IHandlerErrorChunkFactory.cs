using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services;

public interface IHandlerErrorChunkFactory
{
    StreamChunk CreateParseErrorChunk(string errorMessage, int errorCode);
    StreamChunk CreateValidationErrorChunk(string errorMessage, int errorCode);
}