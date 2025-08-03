using System;
using System.Text.Json;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Utils;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Services;

public class ResourceService : IResourceService
{
    private readonly string _docsPath;
    private readonly string _metadataFileName;
    private readonly string[] _supportedExtensions;
    private readonly IMcpLogger _logger;
    private Dictionary<string, string>? _metadata;
    private List<McpResource>? _cachedResources;

    public ResourceService(IMcpLogger logger, ResourceOptions options)
    {
        _logger = logger;
        _docsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, options.Directory));
        _metadataFileName = options.MetadataFileName;
        _supportedExtensions = options.SupportedExtensions.ToArray();

        // docs 폴더가 없으면 생성
        if (!Directory.Exists(_docsPath))
        {
            Directory.CreateDirectory(_docsPath);
            _logger.LogInfo($"Created resources directory: {_docsPath}");
        }
    }

    public async Task<List<McpResource>> GetResourcesAsync()
    {
        if (_cachedResources == null)
        {
            await RefreshResourcesAsync();
        }

        return _cachedResources ?? new List<McpResource>();
    }

    public async Task<McpResourceContent?> ReadResourceAsync(string uri)
    {
        try
        {
            var relativePath = uri.StartsWith(ResourceConstants.FileUriScheme)
                ? uri.Substring(ResourceConstants.FileUriScheme.Length)
                : uri;

            var filePath = Path.Combine(_docsPath, relativePath.Replace(ResourceConstants.UriPathSeparator, Path.DirectorySeparatorChar));

            if (!File.Exists(filePath))
            {
                _logger.LogError($"Resource not found: {uri}");
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath);
            var mimeType = GetMimeType(Path.GetExtension(filePath));

            return new McpResourceContent
            {
                Uri = uri,
                Text = content,
                MimeType = mimeType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read resource: {uri}", uri, ex);
            return null;
        }
    }

    public async Task RefreshResourcesAsync()
    {
        try
        {
            // 메타데이터 로드
            await LoadMetadataAsync();

            // 파일 스캔
            var resources = new List<McpResource>();

            if (Directory.Exists(_docsPath))
            {
                var files = Directory.GetFiles(_docsPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => !Path.GetFileName(f).StartsWith("."))
                    .ToList();

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var relativePath = Path.GetRelativePath(_docsPath, filePath);
                    var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, ResourceConstants.UriPathSeparator); // URI용 정규화
                    var fileInfo = new FileInfo(filePath);

                    resources.Add(new McpResource
                    {
                        Uri = $"{ResourceConstants.FileUriScheme}{normalizedPath}",
                        Name = normalizedPath, // 또는 fileName - 어느 쪽이 더 나을까요?
                        Description = GetFileDescription(normalizedPath),
                        MimeType = GetMimeType(Path.GetExtension(fileName)),
                        Size = fileInfo.Length
                    });
                }
            }

            _cachedResources = resources;
            _logger.LogInfo($"Refreshed resources: found {resources.Count} files");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh resources", null, ex);
            _cachedResources = new List<McpResource>();
        }
    }

    private async Task LoadMetadataAsync()
    {
        var metadataPath = Path.Combine(_docsPath, _metadataFileName);

        if (!File.Exists(metadataPath))
        {
            _metadata = null;
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            _metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            _logger.LogDebug($"Loaded metadata for {_metadata?.Count ?? 0} files");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load metadata: {metadataPath}", null, ex);
            _metadata = null;
        }
    }

    private string GetFileDescription(string normalizedPath)
    {
        // 1. 상대 경로로 메타데이터에서 찾기
        if (_metadata?.TryGetValue(normalizedPath, out var description) == true)
        {
            return description;
        }

        // 2. 파일명만으로도 시도 (슬래시 기준으로 마지막 부분 추출)
        var fileName = normalizedPath.Split(ResourceConstants.UriPathSeparator).Last();
        if (_metadata?.TryGetValue(fileName, out description) == true)
        {
            return description;
        }

        // 3. 기본값
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".md" => ResourceConstants.MimeTypes.Markdown,
            ".txt" => ResourceConstants.MimeTypes.PlainText,
            ".json" => ResourceConstants.MimeTypes.Json,
            ".yaml" or ".yml" => ResourceConstants.MimeTypes.Yaml,
            ".xml" => ResourceConstants.MimeTypes.Xml,
            _ => ResourceConstants.MimeTypes.PlainText
        };
    }
}