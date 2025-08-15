using System;
using System.Reflection;
using Micube.MCP.Core.Loader;
using Micube.MCP.Core.Streamable.Models;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Streamable.Interface;

namespace Micube.MCP.Core.Streamable.Loader;

public class StreamableToolGroupLoader
{
    private readonly IMcpLogger _logger;

    public StreamableToolGroupLoader(IMcpLogger logger)
    {
        _logger = logger;
    }

    public List<LoadedStreamableToolGroup> LoadFromDirectory(string directory, string[]? whitelistDlls = null)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogError($"Tool directory not found: {directory}");
            return new List<LoadedStreamableToolGroup>();
        }

        var dllPaths = Directory
            .EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories)
            .Where(p => whitelistDlls == null || whitelistDlls.Contains(Path.GetFileName(p)))
            .ToList();

        return LoadFromDllPaths(dllPaths);
    }

    private List<LoadedStreamableToolGroup> LoadFromDllPaths(List<string> dllPaths)
    {
        var loadedGroups = new List<LoadedStreamableToolGroup>();

        foreach (var dllPath in dllPaths)
        {
            try
            {
                loadedGroups.AddRange(LoadFromDllFile(dllPath));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load tools from: {dllPath}", ex);
            }
        }

        return loadedGroups;
    }

    private List<LoadedStreamableToolGroup> LoadFromDllFile(string dllPath)
    {
        List<LoadedStreamableToolGroup> loadedGroups = new List<LoadedStreamableToolGroup>();

        var assembly = Assembly.LoadFrom(dllPath);
        var toolGroupTypes = assembly.GetTypes()
            .Where(t => typeof(IStreamableMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in toolGroupTypes)
        {
            var attr = type.GetCustomAttribute<McpToolGroupAttribute>();
            if (attr == null)
            {
                _logger.LogDebug($"Skipped type {type.FullName} (no McpToolGroupAttribute)");
                continue;
            }

            // 생성자 (IMcpLogger) 기반으로 생성
            var ctor = type.GetConstructor(new[] { typeof(IMcpLogger) });
            if (ctor == null)
            {
                _logger.LogError($"Streamable ToolGroup class {type.FullName} has no valid constructor(IMcpLogger)");
                continue;
            }

            var groupInstance = (IStreamableMcpToolGroup)ctor.Invoke(new object[] { _logger });

            var manifestFullPath = Path.Combine(Path.GetDirectoryName(dllPath)!, attr.ManifestPath);
            var metadata = ToolGroupDescriptorParser.Parse(manifestFullPath, _logger);
            groupInstance.Configure(metadata?.Config);

            var loaded = new LoadedStreamableToolGroup
            {
                GroupName = attr.GroupName,
                ManifestPath = manifestFullPath,
                GroupInstance = groupInstance,
                Description = attr.Description ?? "No description provided",
                Metadata = metadata
            };

            loadedGroups.Add(loaded);
            _logger.LogInfo($"Loaded streamable tool group: {loaded.GroupName} from {dllPath}");
        }

        return loadedGroups;
    }
}
