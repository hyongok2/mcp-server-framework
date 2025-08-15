using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Interface;
using Micube.MCP.SDK.Streamable.Models;

namespace Micube.MCP.SDK.Streamable.Abstracts;

/// <summary>
/// Base class for streamable tool groups that extends BaseToolGroup
/// Provides streaming capabilities for MCP tools
/// </summary>
public abstract class BaseStreamableToolGroup : IStreamableMcpToolGroup
{
    public abstract string GroupName { get; }
    protected JsonElement? RawConfig { get; private set; }
    protected IMcpLogger Logger { get; }
    private readonly Dictionary<string, MethodInfo> _toolMethodCache;

    protected BaseStreamableToolGroup(IMcpLogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _toolMethodCache = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
            .ToDictionary(
                m => m.GetCustomAttribute<McpToolAttribute>()!.Name,
                m => m,
                StringComparer.OrdinalIgnoreCase);

        Logger.LogDebug($"Tool group '{GroupName}' initialized with {_toolMethodCache.Count} tools.");
        foreach (var tool in _toolMethodCache)
        {
            Logger.LogDebug($"Tool registered: {tool.Key}, Signature: {tool.Value}");
        }
    }

    public void Configure(JsonElement? config)
    {
        RawConfig = config;
        OnConfigure(config);
    }

    protected abstract void OnConfigure(JsonElement? config);

    // TODO: 아래 함수를 변경해야 함. 스트리밍이 가능한 방식으로. 
    public Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}