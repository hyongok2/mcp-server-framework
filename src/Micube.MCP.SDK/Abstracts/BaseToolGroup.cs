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

    public async Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug($"Invoking tool '{toolName}' in group '{GroupName}' with parameters: {JsonConvert.SerializeObject(parameters, Formatting.None, new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.Default
        })}");

        if (!_toolMethodCache.TryGetValue(toolName, out var method))
            throw new McpException($"Tool '{toolName}' not found in group '{GroupName}'.");

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
            // Task 완료 대기
            await task.ConfigureAwait(false);

            // 패턴 매칭으로 직접 타입 체크
            return task switch
            {
                Task<ToolCallResult> toolCallResultTask => toolCallResultTask.Result,
                Task<string> stringTask => ToolCallResult.Success(stringTask.Result ?? "null"),
                Task<bool> boolTask => ToolCallResult.Success(boolTask.Result.ToString()),
                Task<int> intTask => ToolCallResult.Success(intTask.Result.ToString()),
                Task<long> longTask => ToolCallResult.Success(longTask.Result.ToString()),
                Task<float> floatTask => ToolCallResult.Success(floatTask.Result.ToString()),
                Task<double> doubleTask => ToolCallResult.Success(doubleTask.Result.ToString()),
                Task<object> objectTask => ToolCallResult.Success(JsonConvert.SerializeObject(objectTask.Result ?? "null")),
                _ => HandleGenericTask(task)
            };
        }
        catch (Exception ex)
        {
            return ToolCallResult.Fail($"Tool execution failed: {ex.Message}");
        }
    }

    private static ToolCallResult HandleGenericTask(Task task)
    {
        try
        {
            var taskType = task.GetType();

            // 리플렉션으로 Result 속성 접근
            var resultProperty = taskType.GetProperty("Result");
            if (resultProperty != null)
            {
                var result = resultProperty.GetValue(task);
                return result switch
                {
                    ToolCallResult toolResult => toolResult,
                    string str => ToolCallResult.Success(str ?? "null"),
                    null => ToolCallResult.Success("null"),
                    _ => ToolCallResult.Success(JsonConvert.SerializeObject(result))
                };
            }

            // Result 속성이 없으면 void Task
            return ToolCallResult.Success("Command completed successfully.");
        }
        catch (Exception ex)
        {
            return ToolCallResult.Fail($"Failed to handle generic task: {ex.Message}");
        }
    }

}
