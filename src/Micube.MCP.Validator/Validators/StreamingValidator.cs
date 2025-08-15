using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Streamable.Interface;
using Micube.MCP.SDK.Streamable.Models;
using Micube.MCP.Validator.Models;
using Micube.MCP.Core.Streamable.Models;

namespace Micube.MCP.Validator.Validators;

/// <summary>
/// Validates streaming capabilities of MCP tools
/// </summary>
public class StreamingValidator : IValidator
{
    public string Name => "Streaming Validator";

    public async Task<ValidationReport> ValidateAsync(ValidationContext context)
    {
        var report = new ValidationReport { Context = context };
        var startTime = DateTime.UtcNow;

        if (context.Level < ValidationLevel.Full)
        {
            report.AddInfo("Streaming", "STR001", 
                "Streaming validation skipped (ValidationLevel < Full)");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        if (string.IsNullOrEmpty(context.DllPath))
        {
            report.AddError("Streaming", "STR002", 
                "DLL path is required for streaming validation");
            report.Duration = DateTime.UtcNow - startTime;
            return report;
        }

        try
        {
            // Load assembly
            var assembly = Assembly.LoadFrom(context.DllPath);
            
            // Find streamable tool groups
            var streamableTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && 
                           !t.IsInterface && 
                           typeof(IStreamableMcpToolGroup).IsAssignableFrom(t))
                .ToList();

            if (!streamableTypes.Any())
            {
                report.AddInfo("Streaming", "STR003", 
                    "No streamable tool groups found in assembly");
            }
            else
            {
                report.AddSuccess("Streaming", "STR004", 
                    $"Found {streamableTypes.Count} streamable tool group(s)");

                foreach (var type in streamableTypes)
                {
                    await ValidateStreamableToolGroupAsync(type, report);
                }
            }
        }
        catch (Exception ex)
        {
            report.AddError("Streaming", "STR099", 
                $"Failed to validate streaming capabilities: {ex.Message}");
        }

        report.Duration = DateTime.UtcNow - startTime;
        return report;
    }

    private async Task ValidateStreamableToolGroupAsync(Type toolGroupType, ValidationReport report)
    {
        try
        {
            // Create instance
            var instance = Activator.CreateInstance(toolGroupType);
            if (instance is not IStreamableMcpToolGroup streamableGroup)
            {
                report.AddError("Streaming", "STR010", 
                    $"Failed to cast {toolGroupType.Name} to IStreamableMcpToolGroup");
                return;
            }

            // Validate group name
            if (string.IsNullOrWhiteSpace(streamableGroup.GroupName))
            {
                report.AddError("Streaming", "STR011", 
                    $"{toolGroupType.Name}: GroupName is empty or null");
            }
            else
            {
                report.AddSuccess("Streaming", "STR012", 
                    $"{toolGroupType.Name}: GroupName = '{streamableGroup.GroupName}'");
            }

            // Find streamable methods (methods with IAsyncEnumerable<StreamChunk> return type)
            var streamableMethods = toolGroupType.GetMethods()
                .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null &&
                           IsStreamingMethod(m))
                .ToList();

            if (!streamableMethods.Any())
            {
                report.AddWarning("Streaming", "STR013", 
                    $"{toolGroupType.Name}: No streaming methods found (methods with IAsyncEnumerable<StreamChunk> return type)");
            }
            else
            {
                report.AddSuccess("Streaming", "STR014", 
                    $"{toolGroupType.Name}: Found {streamableMethods.Count} streamable method(s)");

                foreach (var method in streamableMethods)
                {
                    ValidateStreamableMethod(method, toolGroupType.Name, report);
                }
            }

            // Test basic streaming functionality
            await TestStreamingFunctionalityAsync(streamableGroup, streamableMethods, report);
        }
        catch (Exception ex)
        {
            report.AddError("Streaming", "STR020", 
                $"Failed to validate {toolGroupType.Name}: {ex.Message}");
        }
    }

    private void ValidateStreamableMethod(MethodInfo method, string groupName, ValidationReport report)
    {
        var methodName = method.Name;
        
        // Check return type
        var returnType = method.ReturnType;
        if (!returnType.IsGenericType || 
            returnType.GetGenericTypeDefinition() != typeof(IAsyncEnumerable<>) ||
            returnType.GetGenericArguments()[0] != typeof(StreamChunk))
        {
            report.AddError("Streaming", "STR030", 
                $"{groupName}.{methodName}: Return type must be IAsyncEnumerable<StreamChunk>");
            return;
        }

        report.AddSuccess("Streaming", "STR031", 
            $"{groupName}.{methodName}: Valid return type");

        // Check for CancellationToken parameter
        var parameters = method.GetParameters();
        var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));
        
        if (!hasCancellationToken)
        {
            report.AddWarning("Streaming", "STR032", 
                $"{groupName}.{methodName}: No CancellationToken parameter (recommended for streaming)");
        }

        // Check [EnumeratorCancellation] attribute on CancellationToken
        var cancellationParam = parameters.FirstOrDefault(p => p.ParameterType == typeof(CancellationToken));
        if (cancellationParam != null)
        {
            var hasEnumeratorCancellation = cancellationParam
                .GetCustomAttribute<EnumeratorCancellationAttribute>() != null;
            
            if (!hasEnumeratorCancellation)
            {
                report.AddWarning("Streaming", "STR033", 
                    $"{groupName}.{methodName}: CancellationToken should have [EnumeratorCancellation] attribute");
            }
        }

        // Check parameters
        var nonCancellationParams = parameters
            .Where(p => p.ParameterType != typeof(CancellationToken))
            .ToList();

        if (nonCancellationParams.Any())
        {
            report.AddInfo("Streaming", "STR034", 
                $"{groupName}.{methodName}: Has {nonCancellationParams.Count} parameter(s)");
        }
    }

    private async Task TestStreamingFunctionalityAsync(
        IStreamableMcpToolGroup streamableGroup, 
        List<MethodInfo> methods, 
        ValidationReport report)
    {
        if (!methods.Any())
            return;

        var firstMethod = methods.First();
        var methodName = firstMethod.Name;

        try
        {
            // Create test parameters
            var parameters = new Dictionary<string, object>();
            var methodParams = firstMethod.GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken))
                .ToList();

            foreach (var param in methodParams)
            {
                // Add dummy values for testing
                parameters[param.Name!] = GetDefaultValue(param.ParameterType);
            }

            // Test streaming with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var chunks = new List<StreamChunk>();
            var chunkCount = 0;

            await foreach (var chunk in streamableGroup.InvokeStreamAsync(
                methodName, parameters, cts.Token))
            {
                chunks.Add(chunk);
                chunkCount++;

                // Validate chunk
                ValidateStreamChunk(chunk, chunkCount, streamableGroup.GroupName, methodName, report);

                // Limit chunks for testing
                if (chunkCount >= 10)
                {
                    report.AddWarning("Streaming", "STR040", 
                        $"{streamableGroup.GroupName}.{methodName}: Stopped after 10 chunks (test limit)");
                    break;
                }
            }

            if (chunkCount == 0)
            {
                report.AddWarning("Streaming", "STR041", 
                    $"{streamableGroup.GroupName}.{methodName}: No chunks produced");
            }
            else
            {
                report.AddSuccess("Streaming", "STR042", 
                    $"{streamableGroup.GroupName}.{methodName}: Produced {chunkCount} chunk(s)");

                // Check for final chunk
                var hasFinalChunk = chunks.Any(c => c.IsFinal);
                if (!hasFinalChunk)
                {
                    report.AddWarning("Streaming", "STR043", 
                        $"{streamableGroup.GroupName}.{methodName}: No final chunk detected");
                }
            }
        }
        catch (OperationCanceledException)
        {
            report.AddInfo("Streaming", "STR044", 
                $"{streamableGroup.GroupName}.{methodName}: Streaming test timed out (expected for long streams)");
        }
        catch (Exception ex)
        {
            report.AddError("Streaming", "STR045", 
                $"{streamableGroup.GroupName}.{methodName}: Streaming test failed: {ex.Message}");
        }
    }

    private void ValidateStreamChunk(
        StreamChunk chunk, 
        int sequenceNumber, 
        string groupName, 
        string methodName, 
        ValidationReport report)
    {
        // Validate chunk type
        if (!Enum.IsDefined(typeof(StreamChunkType), chunk.Type))
        {
            report.AddError("Streaming", "STR050", 
                $"{groupName}.{methodName}: Invalid chunk type at sequence {sequenceNumber}");
        }

        // Validate content
        if (chunk.Type != StreamChunkType.Error && string.IsNullOrEmpty(chunk.Content))
        {
            report.AddWarning("Streaming", "STR051", 
                $"{groupName}.{methodName}: Empty content at sequence {sequenceNumber}");
        }

        // Validate progress
        if (chunk.Progress.HasValue && (chunk.Progress < 0 || chunk.Progress > 1))
        {
            report.AddWarning("Streaming", "STR052", 
                $"{groupName}.{methodName}: Invalid progress value {chunk.Progress} at sequence {sequenceNumber}");
        }

        // Validate timestamp
        if (chunk.Timestamp == default)
        {
            report.AddWarning("Streaming", "STR053", 
                $"{groupName}.{methodName}: Default timestamp at sequence {sequenceNumber}");
        }
    }

    private object GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return "test";
        if (type == typeof(int))
            return 1;
        if (type == typeof(bool))
            return false;
        if (type == typeof(double))
            return 1.0;
        if (type == typeof(DateTime))
            return DateTime.UtcNow;
        if (type == typeof(Dictionary<string, object>))
            return new Dictionary<string, object>();
        
        return Activator.CreateInstance(type) ?? new object();
    }

    private bool IsStreamingMethod(MethodInfo method)
    {
        var returnType = method.ReturnType;
        return returnType.IsGenericType && 
               returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
               returnType.GetGenericArguments().Length == 1 &&
               returnType.GetGenericArguments()[0].Name == "StreamChunk";
    }
}