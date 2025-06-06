namespace LangBridge.Internal.Abstractions.LanguageModels;

/// <summary>
/// Interface for LLM models optimized for complex reasoning and analysis tasks.
/// </summary>
internal interface IReasoningModel
{
    /// <summary>
    /// Performs reasoning on the given prompt and returns unstructured text response.
    /// </summary>
    /// <param name="prompt">The user prompt to reason about</param>
    /// <param name="systemInstructions">System-level instructions for the model</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The model's reasoning response as text</returns>
    Task<string> ReasonAsync(
        string prompt,
        string systemInstructions,
        CancellationToken cancellationToken = default);
}