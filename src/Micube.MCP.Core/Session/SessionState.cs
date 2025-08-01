using System;

namespace Micube.MCP.Core.Session;

public class SessionState : ISessionState
{
    private readonly object _lock = new object();
    private bool _isInitialized = false;
    
    public bool IsInitialized 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _isInitialized; 
            } 
        } 
    }
    
    public void MarkAsInitialized()
    {
        lock (_lock)
        {
            _isInitialized = true;
        }
    }
    
    public void Reset()
    {
        lock (_lock)
        {
            _isInitialized = false;
        }
    }
}