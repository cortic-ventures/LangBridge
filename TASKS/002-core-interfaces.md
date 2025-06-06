# Task 001: Create Core Interface Abstractions

## Objective
Define the foundational interface contracts for the LangBridge library that enable structured data extraction from unstructured text inputs.

## Background
LangBridge aims to provide atomic, reliable operations for extracting structured insights from text. The interfaces should be minimal, type-safe, and future-proof for multimodal extensions.

## Files to Create

### 1. `/src/LangBridge/Abstractions/IContextualBridge.cs`
Create the base generic interface that all contextual bridges will implement.

```csharp
namespace LangBridge.Abstractions;

/// <summary>
/// Base interface for all contextual bridges that extract structured data from various input types.
/// </summary>
/// <typeparam name="TInput">The type of input to process (e.g., string for text, byte[] for images)</typeparam>
public interface IContextualBridge<TInput>
{
    /// <summary>
    /// Extracts structured data of type T from the provided input based on the query.
    /// </summary>
    /// <typeparam name="T">The type of data to extract</typeparam>
    /// <param name="input">The input to analyze</param>
    /// <param name="query">Natural language query describing what to extract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted data of type T, or null if extraction is not possible</returns>
    Task<T?> ExtractAsync<T>(TInput input, string query, CancellationToken cancellationToken = default);
}
```

### 2. `/src/LangBridge/Abstractions/ITextContextualBridge.cs`
Create the text-specific interface that extends the base interface.

```csharp
namespace LangBridge.Abstractions;

/// <summary>
/// Contextual bridge specifically for processing text inputs and extracting structured data.
/// </summary>
public interface ITextContextualBridge : IContextualBridge<string>
{
    // Inherits ExtractAsync<T>(string input, string query, CancellationToken cancellationToken)
    // Additional text-specific methods can be added here in the future if needed
}
```

## Success Criteria
- Both interfaces compile without errors
- XML documentation is complete and clear
- Interfaces follow C# naming conventions
- Generic constraints are appropriate (consider if T should be constrained)
- The design supports future extensions (audio, image bridges)

## Notes
- Keep interfaces minimal - resist adding methods unless absolutely necessary
- The nullable return type (T?) indicates insufficient information in the input
- Consider whether additional constraints on T are needed (e.g., where T : class)