using CSharpFunctionalExtensions;

namespace LangBridge.ContextualBridging;

/// <summary>
/// Base interface for all contextual bridges that extract structured data from various input types.
/// </summary>
/// <typeparam name="TInput">The type of input to process (e.g., string for text, byte[] for images)</typeparam>
public interface IContextualBridge<TInput>
{
    /// <summary>
    /// Attempts to extract structured data of type T from the provided input based on the query.
    /// Uses all-or-nothing approach - either extracts all properties successfully or fails with detailed error information.
    /// </summary>
    /// <typeparam name="T">The type of data to extract</typeparam>
    /// <param name="input">The input to analyze</param>
    /// <param name="query">Natural language query describing what to extract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing extracted data of type T on success, or detailed failure explanations on error</returns>
    Task<Result<T>> TryFullExtractionAsync<T>(TInput input, string query, CancellationToken cancellationToken = default);
}