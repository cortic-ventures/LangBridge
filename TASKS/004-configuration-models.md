# Task 003: Create Configuration Models

## Objective
Define configuration classes that support multiple LLM providers and model configurations, following the pattern from VeilQuest but simplified for LangBridge needs.

## Background
LangBridge needs flexible configuration to support multiple LLM providers (OpenAI, Anthropic, etc.) and different models for different tasks (reasoning vs tooling). Configuration should be loaded from appsettings.json and support dependency injection.

## Files to Create

### 1. `/src/LangBridge/Configuration/ModelConfig.cs`
Create the model configuration record.

```csharp
namespace LangBridge.Configuration;

/// <summary>
/// Configuration for a specific LLM model.
/// </summary>
public record ModelConfig
{
    /// <summary>
    /// The role this model serves (Reasoning or Tooling).
    /// </summary>
    public required ModelRole Role { get; init; }
    
    /// <summary>
    /// Unique tag to identify this configuration.
    /// </summary>
    public required string Tag { get; init; }
    
    /// <summary>
    /// The LLM provider (OpenAI, Anthropic, etc.).
    /// </summary>
    public required AiProvider Provider { get; init; }
    
    /// <summary>
    /// The specific model name (e.g., "gpt-4-turbo", "claude-3-opus").
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// API key for the provider.
    /// </summary>
    public required string ApiKey { get; init; }
    
    /// <summary>
    /// Optional API endpoint override.
    /// </summary>
    public string? Endpoint { get; init; }
    
    /// <summary>
    /// Optional organization ID (for providers that require it).
    /// </summary>
    public string? OrganizationId { get; init; }
}

/// <summary>
/// Defines the role a model serves in the system.
/// </summary>
public enum ModelRole
{
    /// <summary>
    /// Model used for complex reasoning and analysis.
    /// </summary>
    Reasoning,
    
    /// <summary>
    /// Model used for structured output generation and tool calling.
    /// </summary>
    Tooling
}

/// <summary>
/// Supported AI providers.
/// </summary>
public enum AiProvider
{
    /// <summary>
    /// OpenAI (GPT models).
    /// </summary>
    OpenAI,
    
    /// <summary>
    /// Anthropic (Claude models).
    /// </summary>
    Anthropic,
    
    /// <summary>
    /// Azure OpenAI Service.
    /// </summary>
    AzureOpenAI
}
```

### 2. `/src/LangBridge/Configuration/LangBridgeOptions.cs`
Create the root configuration options class.

```csharp
namespace LangBridge.Configuration;

/// <summary>
/// Configuration options for LangBridge.
/// </summary>
public class LangBridgeOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "LangBridge";
    
    /// <summary>
    /// List of configured models.
    /// </summary>
    public List<ModelConfig> Models { get; set; } = new();
    
    /// <summary>
    /// Gets the model configuration for a specific role.
    /// </summary>
    public ModelConfig? GetModelForRole(ModelRole role)
        => Models.FirstOrDefault(m => m.Role == role);
    
    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (!Models.Any())
            throw new InvalidOperationException("At least one model must be configured");
            
        var roles = Models.Select(m => m.Role).Distinct().ToList();
        
        if (!roles.Contains(ModelRole.Reasoning))
            throw new InvalidOperationException("A reasoning model must be configured");
            
        if (!roles.Contains(ModelRole.Tooling))
            throw new InvalidOperationException("A tooling model must be configured");
    }
}
```

### 3. `/src/LangBridge/Configuration/README.md`
Create documentation for configuration setup.

```markdown
# LangBridge Configuration

## Example appsettings.json

```json
{
  "LangBridge": {
    "Models": [
      {
        "Role": "Reasoning",
        "Tag": "reasoning-primary",
        "Provider": "OpenAI",
        "Model": "gpt-4-turbo-preview",
        "ApiKey": "sk-..."
      },
      {
        "Role": "Tooling",
        "Tag": "tooling-primary",
        "Provider": "OpenAI",
        "Model": "gpt-3.5-turbo",
        "ApiKey": "sk-..."
      }
    ]
  }
}
```

## Environment Variables

API keys can also be set via environment variables:
- `LANGBRIDGE__MODELS__0__APIKEY`
- `LANGBRIDGE__MODELS__1__APIKEY`
```

## Success Criteria
- Configuration models compile without errors
- Support for multiple providers and models
- Clear separation between reasoning and tooling models
- Configuration can be loaded from appsettings.json
- Validation ensures required models are configured
- API keys can be provided securely (environment variables)

## Notes
- Consider adding retry configuration, timeout settings in the future
- Model temperature and other parameters could be added later
- Configuration should be immutable after initialization