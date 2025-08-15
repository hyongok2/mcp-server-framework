using Micube.MCP.Core.Handlers;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.Core.Streamable.Services.Dispatcher;

public interface ISessionValidator
{
    SessionValidationResult ValidateSession(IMethodHandler handler);
    StreamChunk? CreateSessionErrorChunk(SessionValidationResult result, string methodName);
}

public class SessionValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static SessionValidationResult Success() => new() { IsValid = true };
    
    public static SessionValidationResult NotInitialized(string methodName) => 
        new() 
        { 
            IsValid = false, 
            ErrorMessage = $"Method '{methodName}' called before initialization" 
        };
}