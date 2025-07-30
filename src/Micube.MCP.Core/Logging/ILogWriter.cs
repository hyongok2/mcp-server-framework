using System;

namespace Micube.MCP.Core.Logging;

public interface ILogWriter
{
  void Write(LogItem item);
}
