using LangBridge.Internal.Infrastructure.LanguageModels;

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
    public ModelConfig? GetModelForRole(LanguageModelPurposeType role)
        => Models.FirstOrDefault(m => m.Purpose == role);
    
    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (!Models.Any())
            throw new InvalidOperationException("At least one model must be configured");
            
        var roles = Models.Select(m => m.Purpose).Distinct().ToList();
        
        if (!roles.Contains(LanguageModelPurposeType.Reasoning))
            throw new InvalidOperationException("A reasoning model must be configured");
            
        if (!roles.Contains(LanguageModelPurposeType.Tooling))
            throw new InvalidOperationException("A tooling model must be configured");
    }
}