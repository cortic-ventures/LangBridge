namespace LangBridge.ContextualBridging;

/// <summary>
/// Contextual bridge specifically for processing text inputs and extracting structured data.
/// </summary>
public interface ITextContextualBridge : IContextualBridge<string>
{
    // Inherits ExtractAsync<T>(string input, string query, CancellationToken cancellationToken)
    // Additional text-specific methods can be added here in the future if needed
}