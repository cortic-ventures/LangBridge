using LangBridge.Internal.Infrastructure.LanguageModels;

namespace LangBridge.Configuration;

/// <summary>
/// Configuration for a specific LLM model.
/// </summary>
public record ModelConfig
{
    
    /// <summary>
    /// Unique tag to identify this configuration.
    /// </summary>
    public required LanguageModelPurposeType Purpose { get; init; }
    
    /// <summary>
    /// The LLM provider (OpenAI, Anthropic, etc.).
    /// </summary>
    public required AiProvider Provider { get; init; }
    
    /// <summary>
    /// The specific model name (e.g., "gpt-4-turbo", "claude-3-opus").
    /// </summary>
    public required string ModelName { get; init; }
    
    /// <summary>
    /// API key for the provider.
    /// </summary>
    public required string ApiKey { get; init; }
    
    /// <summary>
    /// Optional API endpoint override.
    /// </summary>
    public string? Endpoint { get; init; }
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
    OpenAI,
    AzureOpenAI,
    OpenRouter,
    Ollama,
    Groq
}