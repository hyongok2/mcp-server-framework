using System.Reflection;

namespace Micube.MCP.SDK.Streamable.Services;

public interface IToolMethodRegistry
{
    Dictionary<string, MethodInfo> DiscoverToolMethods(Type toolGroupType);
    MethodInfo? GetToolMethod(string toolName);
}

public class ToolMethodInfo
{
    public string Name { get; init; } = string.Empty;
    public MethodInfo Method { get; init; } = null!;
    public bool IsStreaming { get; init; }
}