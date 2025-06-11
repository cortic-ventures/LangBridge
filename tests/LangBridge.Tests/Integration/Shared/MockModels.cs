using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Infrastructure.Processing;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Mock implementation of IReasoningModel for deterministic testing.
/// </summary>
public class MockReasoningModel : IReasoningModel
{
    private readonly Dictionary<string, string> _responses = new();
    private readonly Queue<string> _defaultResponses = new();
    private string _fallbackResponse = "YES: Information available";

    public Task<string> ReasonAsync(string prompt, string systemInstructions, CancellationToken cancellationToken = default)
    {
        // Check cancellation before processing
        cancellationToken.ThrowIfCancellationRequested();

        // Check for exact prompt match first
        if (_responses.TryGetValue(prompt, out var response))
        {
            return Task.FromResult(response);
        }

        // Check for pattern-based responses
        foreach (var kvp in _responses)
        {
            if (prompt.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(kvp.Value);
            }
        }

        // Use queued responses if available
        if (_defaultResponses.Count > 0)
        {
            return Task.FromResult(_defaultResponses.Dequeue());
        }

        // Use fallback
        return Task.FromResult(_fallbackResponse);
    }

    /// <summary>
    /// Configures a response for prompts containing a specific key phrase.
    /// </summary>
    public MockReasoningModel WithResponseForKey(string keyPhrase, string response)
    {
        _responses[keyPhrase] = response;
        return this;
    }

    /// <summary>
    /// Configures multiple sequential default responses (used when no specific match found).
    /// </summary>
    public MockReasoningModel WithDefaultResponses(params string[] responses)
    {
        _defaultResponses.Clear();
        foreach (var response in responses)
        {
            _defaultResponses.Enqueue(response);
        }
        return this;
    }

    /// <summary>
    /// Clears all configured responses.
    /// </summary>
    public MockReasoningModel Reset()
    {
        _responses.Clear();
        _defaultResponses.Clear();
        _fallbackResponse = "YES: Information available";
        return this;
    }
}

/// <summary>
/// Mock implementation of IDataStructuringModel for deterministic testing.
/// </summary>
public class MockDataStructuringModel : IDataStructuringModel
{
    private readonly Dictionary<Type, object?> _responses = new();
    private object? _defaultResponse;

    public Task<T?> GenerateStructuredAsync<T>(string prompt, CancellationToken cancellationToken = default)
    {
        // Check cancellation before processing
        cancellationToken.ThrowIfCancellationRequested();

        // Check for type-specific response
        if (_responses.TryGetValue(typeof(T), out var response))
        {
            return Task.FromResult((T?)response);
        }

        // Use default response if configured
        if (_defaultResponse != null && _defaultResponse is T defaultTypedResponse)
        {
            return Task.FromResult((T?)defaultTypedResponse);
        }

        // Return null for failures
        return Task.FromResult(default(T));
    }

    /// <summary>
    /// Configures a response for a specific type.
    /// </summary>
    public MockDataStructuringModel WithResponse<T>(T? response)
    {
        _responses[typeof(T)] = response;
        return this;
    }

    /// <summary>
    /// Configures a default response for any type (if no specific response configured).
    /// </summary>
    public MockDataStructuringModel WithDefaultResponse(object? response)
    {
        _defaultResponse = response;
        return this;
    }

    /// <summary>
    /// Configures the model to return null for all requests (simulates failures).
    /// </summary>
    public MockDataStructuringModel WithAllFailures()
    {
        _responses.Clear();
        _defaultResponse = null;
        return this;
    }

    /// <summary>
    /// Clears all configured responses.
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        _defaultResponse = null;
    }
}