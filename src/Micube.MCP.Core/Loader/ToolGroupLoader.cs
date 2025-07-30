using System.Reflection;
using Micube.MCP.Core.Models;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Loader;

public class ToolGroupLoader
{
    private readonly IMcpLogger _logger;

    public ToolGroupLoader(IMcpLogger logger)
    {
        _logger = logger;
    }

    public List<LoadedToolGroup> LoadFromDirectory(string directory, string[]? whitelistDlls = null)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogError($"Tool directory not found: {directory}");
            return new List<LoadedToolGroup>();
        }

        var dllPaths = Directory
            .EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(p => whitelistDlls == null || whitelistDlls.Contains(Path.GetFileName(p)))
            .ToList();

        return LoadFromDllPaths(dllPaths);
    }

    private List<LoadedToolGroup> LoadFromDllPaths(List<string> dllPaths)
    {
        var loadedGroups = new List<LoadedToolGroup>();

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

    private List<LoadedToolGroup> LoadFromDllFile(string dllPath)
    {
        List<LoadedToolGroup> loadedGroups = new List<LoadedToolGroup>();

        var assembly = Assembly.LoadFrom(dllPath);
        var toolGroupTypes = assembly.GetTypes()
            .Where(t => typeof(IMcpToolGroup).IsAssignableFrom(t) && !t.IsAbstract);

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
                _logger.LogError($"ToolGroup class {type.FullName} has no valid constructor(IMcpLogger)");
                continue;
            }

            var groupInstance = (IMcpToolGroup)ctor.Invoke(new object[] { _logger });

            var manifestFullPath = Path.Combine(Path.GetDirectoryName(dllPath)!, attr.ManifestPath);
            var metadata = ToolGroupDescriptorParser.Parse(manifestFullPath, _logger);
            groupInstance.Configure(metadata?.Config);

            var loaded = new LoadedToolGroup
            {
                GroupName = attr.GroupName,
                ManifestPath = manifestFullPath,
                GroupInstance = groupInstance,
                Description = attr.Description ?? "No description provided",
                Metadata = metadata
            };

            loadedGroups.Add(loaded);
            _logger.LogInfo($"Loaded tool group: {loaded.GroupName} from {dllPath}");
        }

        return loadedGroups;
    }
}
