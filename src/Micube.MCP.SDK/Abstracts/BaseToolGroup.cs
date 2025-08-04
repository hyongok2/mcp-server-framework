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

        return await ConvertTaskResultAsync(task);
    }

    public void Configure(JsonElement? config)
    {
        RawConfig = config;
        OnConfigure(config);
    }

    protected abstract void OnConfigure(JsonElement? config);

    /// <summary>
    /// Task의 결과를 ToolCallResult로 변환합니다.
    /// </summary>
    private static async Task<ToolCallResult> ConvertTaskResultAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);

            // 패턴 매칭으로 모든 케이스를 한 번에 처리!
            return task switch
            {
                // 특수 타입
                Task<ToolCallResult> toolCallResultTask => toolCallResultTask.Result,
                // 기본 타입들
                Task<string> stringTask => ToolCallResult.Success(stringTask.Result ?? "null"),
                Task<bool> boolTask => ToolCallResult.Success(boolTask.Result.ToString()),
                Task<int> intTask => ToolCallResult.Success(intTask.Result.ToString()),
                Task<long> longTask => ToolCallResult.Success(longTask.Result.ToString()),
                Task<float> floatTask => ToolCallResult.Success(floatTask.Result.ToString()),
                Task<double> doubleTask => ToolCallResult.Success(doubleTask.Result.ToString()),
                Task<decimal> decimalTask => ToolCallResult.Success(decimalTask.Result.ToString()),
                Task<byte> byteTask => ToolCallResult.Success(byteTask.Result.ToString()),
                Task<short> shortTask => ToolCallResult.Success(shortTask.Result.ToString()),
                Task<uint> uintTask => ToolCallResult.Success(uintTask.Result.ToString()),
                Task<ulong> ulongTask => ToolCallResult.Success(ulongTask.Result.ToString()),
                Task<ushort> ushortTask => ToolCallResult.Success(ushortTask.Result.ToString()),
                Task<sbyte> sbyteTask => ToolCallResult.Success(sbyteTask.Result.ToString()),
                Task<object> objectTask => ToolCallResult.Success(SerializeObject(objectTask.Result)),
                Task _ => ToolCallResult.Success("Command completed successfully."),

                // 이론상 도달할 수 없는 케이스 (Task가 아닌 경우)
                _ => ToolCallResult.Fail("Invalid task type")
            };
        }
        catch (Exception ex)
        {
            return ToolCallResult.Fail($"Tool execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 객체를 JSON으로 직렬화합니다.
    /// </summary>
    private static string SerializeObject(object? obj)
    {
        if (obj == null) return "null";

        try
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });
        }
        catch (Exception ex)
        {
            // JSON 직렬화 실패 시 ToString() 사용
            return $"{{\"error\":\"Serialization failed: {ex.Message}\", \"toString\":\"{obj}\"}}";
        }
    }
}