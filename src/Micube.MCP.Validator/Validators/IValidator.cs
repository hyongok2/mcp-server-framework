using Micube.MCP.Validator.Models;

namespace Micube.MCP.Validator.Validators;

public interface IValidator
{
    string Name { get; }
    Task<ValidationReport> ValidateAsync(ValidationContext context);
}