using Micube.MCP.Core.Handlers;
using Micube.MCP.Core.Session;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public class SessionValidator : ISessionValidator
{
    private readonly ISessionState _sessionState;

    public SessionValidator(ISessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public SessionValidationResult ValidateSession(IMethodHandler handler)
    {
        if (handler.RequiresInitialization && !_sessionState.IsInitialized)
        {
            return SessionValidationResult.NotInitialized(handler.MethodName);
        }

        return SessionValidationResult.Success();
    }

    public StreamChunk? CreateSessionErrorChunk(SessionValidationResult result, string methodName)
    {
        if (result.IsValid)
            return null;

        return new StreamChunk
        {
            Type = StreamChunkType.Error,
            Content = "Server not initialized. Call initialize first",
            IsFinal = true,
            Timestamp = DateTime.UtcNow
        };
    }
}