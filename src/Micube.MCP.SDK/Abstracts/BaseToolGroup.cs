using System;
using System.Reflection;
using System.Text.Json;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Newtonsoft.Json;

namespace Micube.MCP.SDK.Abstracts;

public abstract class BaseToolGroup : IMcpToolGroup
{
    public abstract string GroupName { get; }
    protected JsonElement? RawConfig { get; private set; }
    protected IMcpLogger Logger { get; }
    private readonly Dictionary<string, MethodInfo> _toolMethodCache;

    protected BaseToolGroup(IMcpLogger logger)
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

    public async Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters)
    {
        Logger.LogDebug($"Invoking tool '{toolName}' in group '{GroupName}' with parameters: {JsonConvert.SerializeObject(parameters, Formatting.None, new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.Default
        })}");

        if (!_toolMethodCache.TryGetValue(toolName, out var method))
            throw new McpToolNotFoundException($"Tool '{toolName}' not found in group '{GroupName}'.");

        var result = method.Invoke(this, new object[] { parameters });

        if (result is not Task task)
            return ToolCallResult.Fail("Tool did not return a Task. Tools must be async methods.");

        return await CastAsync(task);
    }
    public void Configure(JsonElement? config)
    {
        RawConfig = config;
        OnConfigure(config);
    }

    protected abstract void OnConfigure(JsonElement? config);

    private static async Task<ToolCallResult> CastAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);

            var type = task.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var result = ((dynamic)task).Result;

                return result switch
                {
                    ToolCallResult toolResult => toolResult,
                    string str => ToolCallResult.Success(str),
                    _ => ToolCallResult.Success(JsonConvert.SerializeObject(result))
                };
            }

            return ToolCallResult.Success("No return value from tool."); // Task만 반환된 경우
        }
        catch (Exception ex)
        {
            return ToolCallResult.Fail($"Tool execution failed: {ex.Message}");
        }
    }

}
