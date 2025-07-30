using System;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Logging;

/// <summary>
/// 다양한 방식으로 로깅할 수 있도록 하는 로깅 디스패처입니다.
/// 이 클래스는 IMcpLogger 인터페이스를 구현하여 로깅 기능을 제공합니다
/// </summary>
public class LogDispatcher : IMcpLogger
{
    public void LogDebug(string message)
    {
        throw new NotImplementedException();
    }

    public void LogError(string message, Exception? ex = null)
    {
        throw new NotImplementedException();
    }

    public void LogInfo(string message)
    {
        throw new NotImplementedException();
    }
}
