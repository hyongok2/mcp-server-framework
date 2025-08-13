namespace Micube.MCP.Validator.Models;

public class ValidationContext
{
    public string? DllPath { get; set; }
    public string? ManifestPath { get; set; }
    public string? DirectoryPath { get; set; }
    public bool StrictMode { get; set; }
    public ValidationLevel Level { get; set; } = ValidationLevel.Full;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ValidationLevel
{
    Basic,      // 구조 검증만
    Standard,   // 구조 + 규격 검증
    Full        // 구조 + 규격 + 실행 검증
}