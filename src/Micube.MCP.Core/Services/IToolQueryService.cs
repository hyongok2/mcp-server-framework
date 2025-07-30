using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Services;

public interface IToolQueryService
{
    List<McpToolInfo> GetAllTools();
}
