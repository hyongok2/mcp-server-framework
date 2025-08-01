using System;
using Micube.MCP.Core.Models;

namespace Micube.MCP.Core.Validation;

public interface IMessageValidator
{
    ValidationResult Validate(McpMessage message);
}
