// src/Micube.MCP.Core/Services/PromptService.cs
using System.Text.Json;
using Micube.MCP.Core.Models;
using Micube.MCP.Core.Options;
using Micube.MCP.Core.Validation;
using Micube.MCP.SDK.Exceptions;
using Micube.MCP.SDK.Interfaces;

namespace Micube.MCP.Core.Services;

public class PromptService : IPromptService
{
    private readonly string _promptsPath;
    private readonly IMcpLogger _logger;
    
    // 캐시
    private List<McpPrompt>? _cachedPrompts;
    private readonly Dictionary<string, PromptDefinition> _promptDefinitions = new();
    private readonly Dictionary<string, string> _templateCache = new();

    public PromptService(IMcpLogger logger, PromptOptions options)
    {
        _logger = logger;
        _promptsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, options.Directory));

        // prompts 폴더가 없으면 생성
        if (!Directory.Exists(_promptsPath))
        {
            Directory.CreateDirectory(_promptsPath);
            _logger.LogInfo($"Created prompts directory: {_promptsPath}");
        }
    }

    public async Task<List<McpPrompt>> GetPromptsAsync()
    {
        if (_cachedPrompts == null)
        {
            await LoadPromptsAsync();
        }

        return _cachedPrompts ?? new List<McpPrompt>();
    }

    public async Task<McpPromptResponse?> GetPromptAsync(string name, Dictionary<string, object>? arguments = null)
    {
        try
        {
            // 1. 프롬프트 정의 찾기
            if (!_promptDefinitions.TryGetValue(name, out var definition))
            {
                _logger.LogError($"Prompt not found: {name}");
                return null;
            }

            // 2. 매개변수 검증
            var validationResult = ValidateArguments(definition, arguments ?? new Dictionary<string, object>());
            if (!validationResult.IsValid)
            {
                _logger.LogError($"Argument validation failed for '{name}': Invalid parameters");
                return null;
            }

            // 3. 템플릿 로드
            var template = await LoadTemplateAsync(definition);
            if (template == null)
            {
                return null;
            }

            // 4. 템플릿 렌더링
            var renderedText = RenderTemplate(template, arguments ?? new Dictionary<string, object>());

            // 5. MCP 응답 구성
            return new McpPromptResponse
            {
                Description = definition.Description,
                Messages = new List<McpPromptMessage>
                {
                    new McpPromptMessage
                    {
                        Role = "user",
                        Content = new McpPromptContent
                        {
                            Type = "text",
                            Text = renderedText
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get prompt '{name}': {ex.Message}", null, ex);
            return null;
        }
    }

    /// <summary>
    /// 프롬프트 정의들을 로드합니다
    /// </summary>
    private async Task LoadPromptsAsync()
    {
        try
        {
            var prompts = new List<McpPrompt>();
            _promptDefinitions.Clear();

            if (!Directory.Exists(_promptsPath))
            {
                _logger.LogError($"Prompts directory not found: {_promptsPath}");
                _cachedPrompts = prompts;
                return;
            }

            // JSON 파일들 스캔
            var jsonFiles = Directory.GetFiles(_promptsPath, "*.json", SearchOption.AllDirectories);
            _logger.LogInfo($"Found {jsonFiles.Length} prompt definition files");

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(jsonFile);
                    var definition = JsonSerializer.Deserialize<PromptDefinition>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });

                    if (definition == null || string.IsNullOrEmpty(definition.Name))
                    {
                        _logger.LogError($"Invalid prompt definition in file: {jsonFile}");
                        continue;
                    }

                    // 중복 이름 검사
                    if (_promptDefinitions.ContainsKey(definition.Name))
                    {
                        _logger.LogError($"Duplicate prompt name '{definition.Name}' found in: {jsonFile}");
                        continue;
                    }

                    _promptDefinitions[definition.Name] = definition;
                    prompts.Add(definition.ToMcpPrompt());

                    _logger.LogDebug($"Loaded prompt: {definition.Name} from {Path.GetFileName(jsonFile)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to parse prompt definition: {jsonFile}", null, ex);
                }
            }

            _cachedPrompts = prompts;
            _logger.LogInfo($"Successfully loaded {prompts.Count} prompts");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load prompts", null, ex);
            _cachedPrompts = new List<McpPrompt>();
        }
    }

    /// <summary>
    /// 템플릿 파일을 로드합니다 (캐싱 포함)
    /// </summary>
    private async Task<string?> LoadTemplateAsync(PromptDefinition definition)
    {
        var cacheKey = $"{definition.Name}:{definition.TemplateFile}";
        
        // 캐시에서 확인
        if (_templateCache.TryGetValue(cacheKey, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        try
        {
            // 템플릿 파일 경로 계산
            var templatePath = Path.Combine(_promptsPath, definition.TemplateFile);
            
            if (!File.Exists(templatePath))
            {
                _logger.LogError($"Template file not found: {templatePath} for prompt '{definition.Name}'");
                return null;
            }

            var template = await File.ReadAllTextAsync(templatePath);
            
            // 캐시에 저장
            _templateCache[cacheKey] = template;
            
            _logger.LogDebug($"Loaded template for '{definition.Name}': {definition.TemplateFile}");
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load template for '{definition.Name}': {ex.Message}", null, ex);
            return null;
        }
    }

    /// <summary>
    /// 템플릿을 렌더링합니다 (단순한 {변수} 치환)
    /// </summary>
    private static string RenderTemplate(string template, Dictionary<string, object> arguments)
    {
        var result = template;

        foreach (var arg in arguments)
        {
            var placeholder = $"{{{arg.Key}}}";
            var value = arg.Value?.ToString() ?? "";
            result = result.Replace(placeholder, value);
        }

        return result;
    }

    /// <summary>
    /// 매개변수를 검증합니다
    /// </summary>
    private ValidationResult ValidateArguments(PromptDefinition definition, Dictionary<string, object> arguments)
    {
        if (definition.Arguments == null || definition.Arguments.Count == 0)
        {
            return ValidationResult.Success();
        }

        // 필수 매개변수 체크
        foreach (var arg in definition.Arguments)
        {
            if (arg.Required == true && !arguments.ContainsKey(arg.Name))
            {
                return ValidationResult.Error(McpErrorCodes.INVALID_PARAMS, 
                    "Required argument missing", arg.Name);
            }
        }

        return ValidationResult.Success();
    }
}