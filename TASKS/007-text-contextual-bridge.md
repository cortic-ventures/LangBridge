# Task 006: Implement Text Contextual Bridge

## Objective
Create the main TextContextualBridge implementation that orchestrates reasoning and tooling models to extract structured data from text reliably.

## Background
This is the core implementation that users will interact with. It should intelligently decide when to use reasoning vs tooling models and implement strategies for reliable, deterministic extraction.

## Files to Create

### 1. `/src/LangBridge/Implementation/TextContextualBridge.cs`
Create the main bridge implementation (scaffold only - core logic to be implemented separately).

```csharp
namespace LangBridge.Implementation;

using LangBridge.Abstractions;

/// <summary>
/// Implementation of text contextual bridge for extracting structured data from text.
/// </summary>
public class TextContextualBridge : ITextContextualBridge
{
    private readonly IReasoningModel _reasoningModel;
    private readonly IToolingModel _toolingModel;
    
    public TextContextualBridge(
        IReasoningModel reasoningModel,
        IToolingModel toolingModel)
    {
        _reasoningModel = reasoningModel ?? throw new ArgumentNullException(nameof(reasoningModel));
        _toolingModel = toolingModel ?? throw new ArgumentNullException(nameof(toolingModel));
    }
    
    /// <inheritdoc/>
    public async Task<T?> ExtractAsync<T>(
        string input, 
        string query, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace", nameof(input));
            
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace", nameof(query));
        
        // TODO: Core implementation to be provided by user
        // This is where the intelligent extraction logic will go:
        // 1. Determine if simple extraction or complex reasoning is needed
        // 2. Build appropriate prompts
        // 3. Handle retries for consistency
        // 4. Validate responses
        
        throw new NotImplementedException("Core extraction logic to be implemented");
    }
    
    /// <summary>
    /// Determines if the type requires complex reasoning or can use direct extraction.
    /// </summary>
    private bool RequiresReasoning<T>()
    {
        // TODO: Implement logic to determine if reasoning is needed
        // For now, use simple heuristic
        var type = typeof(T);
        
        // Simple types can often be extracted directly
        if (IsSimpleType(type))
            return false;
            
        // Complex objects might benefit from reasoning first
        return true;
    }
    
    /// <summary>
    /// Checks if a type is considered "simple" for extraction purposes.
    /// </summary>
    private bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.IsPrimitive || 
               underlyingType == typeof(string) || 
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(Guid) ||
               underlyingType.IsEnum;
    }
    
    /// <summary>
    /// Builds the system instructions for extraction based on the target type.
    /// </summary>
    private string BuildSystemInstructions<T>(bool forReasoning)
    {
        // TODO: Build appropriate system instructions
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Builds the user prompt combining the input text and query.
    /// </summary>
    private string BuildPrompt(string input, string query, bool forReasoning)
    {
        // TODO: Build appropriate prompt
        throw new NotImplementedException();
    }
}
```

### 2. `/src/LangBridge/Extensions/TypeExtensions.cs`
Create helper extensions for type checking.

```csharp
namespace LangBridge.Extensions;

/// <summary>
/// Extension methods for Type operations.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines if a type is numeric.
    /// </summary>
    public static bool IsNumericType(this Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(decimal);
    }
    
    /// <summary>
    /// Gets a human-readable description of the type for prompts.
    /// </summary>
    public static string GetFriendlyName(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return $"{Nullable.GetUnderlyingType(type)?.GetFriendlyName()} (optional)";
        }
        
        var aliases = new Dictionary<Type, string>
        {
            { typeof(bool), "boolean (true/false)" },
            { typeof(int), "integer number" },
            { typeof(decimal), "decimal number" },
            { typeof(string), "text string" },
            { typeof(DateTime), "date and time" },
            { typeof(Guid), "unique identifier (GUID)" }
        };
        
        return aliases.TryGetValue(type, out var alias) ? alias : type.Name;
    }
}
```

## Success Criteria
- Bridge implementation compiles with all method signatures correct
- Proper null checking and argument validation
- Clear TODO markers for core logic implementation
- Helper methods for type analysis are in place
- Extension methods support common scenarios

## Notes
- The actual ExtractAsync implementation is left as TODO for the user
- Consider adding logging interfaces in the future
- Retry logic and consistency checking will be crucial for reliability
- The bridge should be stateless and thread-safe