namespace LangBridge.Internal.Abstractions.LanguageModels;

/// <summary>
/// Interface for LLM models optimized for generating structured outputs and tool calling.
/// </summary>
internal interface IDataStructuringModel
{
    /// <summary>
    /// Generates structured data of type T based on the prompt.
    /// </summary>
    /// <typeparam name="T">The type of structured data to generate</typeparam>
    /// <param name="prompt">The prompt describing what to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated structured data, or null if generation fails</returns>
    Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        CancellationToken cancellationToken = default);
}