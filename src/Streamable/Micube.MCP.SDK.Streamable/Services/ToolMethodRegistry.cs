using System.Reflection;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Services;

public class ToolMethodRegistry : IToolMethodRegistry
{
    private readonly Dictionary<string, MethodInfo> _toolMethodCache;
    private readonly IMcpLogger _logger;
    private readonly string _groupName;

    public ToolMethodRegistry(Type toolGroupType, IMcpLogger logger, string groupName)
    {
        _logger = logger;
        _groupName = groupName;
        _toolMethodCache = DiscoverToolMethods(toolGroupType);
        
        LogDiscoveredMethods();
    }

    public Dictionary<string, MethodInfo> DiscoverToolMethods(Type toolGroupType)
    {
        return toolGroupType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
            .ToDictionary(
                m => m.GetCustomAttribute<McpToolAttribute>()!.Name,
                m => m,
                StringComparer.OrdinalIgnoreCase);
    }

    public MethodInfo? GetToolMethod(string toolName)
    {
        _toolMethodCache.TryGetValue(toolName, out var method);
        return method;
    }

    private void LogDiscoveredMethods()
    {
        _logger.LogDebug($"Tool group '{_groupName}' initialized with {_toolMethodCache.Count} tools.");
        foreach (var tool in _toolMethodCache)
        {
            _logger.LogDebug($"Tool registered: {tool.Key}, Signature: {tool.Value}");
        }
    }
}