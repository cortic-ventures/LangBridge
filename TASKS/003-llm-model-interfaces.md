# Task 002: Create LLM Model Abstraction Interfaces

## Objective
Define interfaces that abstract away Semantic Kernel and provide clear contracts for different types of LLM operations (reasoning vs structured output generation).

## Background
Different LLM tasks require different capabilities. Complex analysis needs reasoning models, while structured data extraction benefits from tool-calling models. These interfaces hide Semantic Kernel implementation details, making future migrations easier.

## Files to Create

### 1. `/src/LangBridge/Abstractions/IReasoningModel.cs`
Create interface for models that perform complex reasoning and analysis.

```csharp
namespace LangBridge.Abstractions;

/// <summary>
/// Interface for LLM models optimized for complex reasoning and analysis tasks.
/// </summary>
public interface IReasoningModel
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
```

### 2. `/src/LangBridge/Abstractions/IToolingModel.cs`
Create interface for models that generate structured outputs.

```csharp
namespace LangBridge.Abstractions;

/// <summary>
/// Interface for LLM models optimized for generating structured outputs and tool calling.
/// </summary>
public interface IToolingModel
{
    /// <summary>
    /// Generates structured data of type T based on the prompt.
    /// </summary>
    /// <typeparam name="T">The type of structured data to generate</typeparam>
    /// <param name="prompt">The prompt describing what to generate</param>
    /// <param name="systemInstructions">System-level instructions for structured output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated structured data, or null if generation fails</returns>
    Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        string systemInstructions,
        CancellationToken cancellationToken = default);
}
```

### 3. `/src/LangBridge/Abstractions/IKernelFactory.cs`
Create factory interface for creating Semantic Kernel instances.

```csharp
namespace LangBridge.Abstractions;

using Microsoft.SemanticKernel;

/// <summary>
/// Factory for creating configured Semantic Kernel instances.
/// </summary>
public interface IKernelFactory
{
    /// <summary>
    /// Creates a kernel configured for reasoning tasks.
    /// </summary>
    /// <returns>Configured kernel instance</returns>
    Kernel CreateReasoningKernel();

    /// <summary>
    /// Creates a kernel configured for structured output/tooling tasks.
    /// </summary>
    /// <returns>Configured kernel instance</returns>
    Kernel CreateToolingKernel();
}
```

## Success Criteria
- All interfaces compile without errors
- Clear separation between reasoning and tooling capabilities
- Interfaces don't leak Semantic Kernel details (except IKernelFactory)
- Async methods follow TAP pattern with CancellationToken
- XML documentation explains the purpose of each interface

## Notes
- IKernelFactory is the only interface that knows about Semantic Kernel
- Consider if additional methods are needed (e.g., streaming support)
- These interfaces will be implemented using Semantic Kernel initially but could be swapped later