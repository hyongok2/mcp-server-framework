using System;

namespace Micube.MCP.Core.Session;

public interface ISessionState
{
    bool IsInitialized { get; }
    void MarkAsInitialized();
    void Reset();
}