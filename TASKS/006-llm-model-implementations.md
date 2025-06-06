# Task 005: Implement LLM Model Abstractions

## Objective
Create the Semantic Kernel-based implementations of IReasoningModel and IToolingModel that hide SK complexity and provide clean APIs for text generation and structured output.

## Background
These implementations wrap Semantic Kernel functionality to provide a clean abstraction layer. They should handle prompt construction, error handling, and response parsing while keeping the interface simple.

## Files to Create

### 1. `/src/LangBridge/Implementation/SemanticKernelReasoningModel.cs`
Create the reasoning model implementation.

```csharp
namespace LangBridge.Implementation;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using LangBridge.Abstractions;

/// <summary>
/// Semantic Kernel implementation of the reasoning model.
/// </summary>
public class SemanticKernelReasoningModel : IReasoningModel
{
    private readonly IKernelFactory _kernelFactory;
    
    public SemanticKernelReasoningModel(IKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }
    
    /// <inheritdoc/>
    public async Task<string> ReasonAsync(
        string prompt,
        string systemInstructions,
        CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.CreateReasoningKernel();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemInstructions);
        chatHistory.AddUserMessage(prompt);
        
        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);
            
        return response.Content ?? string.Empty;
    }
}
```

### 2. `/src/LangBridge/Implementation/SemanticKernelToolingModel.cs`
Create the tooling model implementation with structured output support.

```csharp
namespace LangBridge.Implementation;

using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using LangBridge.Abstractions;

/// <summary>
/// Semantic Kernel implementation of the tooling model for structured outputs.
/// </summary>
public class SemanticKernelToolingModel : IToolingModel
{
    private readonly IKernelFactory _kernelFactory;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public SemanticKernelToolingModel(IKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    /// <inheritdoc/>
    public async Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        string systemInstructions,
        CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.CreateToolingKernel();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        // Enhance system instructions for structured output
        var enhancedInstructions = EnhanceInstructionsForStructuredOutput<T>(systemInstructions);
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(enhancedInstructions);
        chatHistory.AddUserMessage(prompt);
        
        // Configure for JSON output if supported
        var executionSettings = new PromptExecutionSettings
        {
            // Enable JSON mode if available (OpenAI specific)
            ResponseFormat = "json_object"
        };
        
        try
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                cancellationToken: cancellationToken);
                
            if (string.IsNullOrWhiteSpace(response.Content))
                return default;
                
            // Handle simple types directly
            if (IsSimpleType<T>())
            {
                return ParseSimpleType<T>(response.Content);
            }
            
            // Parse JSON for complex types
            return JsonSerializer.Deserialize<T>(response.Content, _jsonOptions);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return null (insufficient information)
            return default;
        }
        catch (NotSupportedException)
        {
            // Fallback if JSON mode is not supported
            return await GenerateStructuredFallbackAsync<T>(
                kernel, 
                chatService, 
                chatHistory, 
                cancellationToken);
        }
    }
    
    private string EnhanceInstructionsForStructuredOutput<T>(string baseInstructions)
    {
        var typeName = typeof(T).Name;
        var isSimple = IsSimpleType<T>();
        
        if (isSimple)
        {
            return $@"{baseInstructions}

Respond with only the {typeName} value, nothing else.
If the information cannot be determined, respond with 'null'.";
        }
        
        var schema = GenerateSimpleSchema<T>();
        return $@"{baseInstructions}

Respond with valid JSON that matches this structure:
{schema}

If the information cannot be determined with confidence, respond with null.";
    }
    
    private bool IsSimpleType<T>()
    {
        var type = typeof(T);
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)!));
    }
    
    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid);
    }
    
    private T? ParseSimpleType<T>(string content)
    {
        var trimmed = content.Trim().Trim('"');
        
        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            return default;
            
        try
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            if (underlyingType == typeof(bool))
                return (T)(object)bool.Parse(trimmed);
            if (underlyingType == typeof(int))
                return (T)(object)int.Parse(trimmed);
            if (underlyingType == typeof(decimal))
                return (T)(object)decimal.Parse(trimmed);
            if (underlyingType == typeof(DateTime))
                return (T)(object)DateTime.Parse(trimmed);
            if (underlyingType == typeof(string))
                return (T)(object)trimmed;
                
            return default;
        }
        catch
        {
            return default;
        }
    }
    
    private string GenerateSimpleSchema<T>()
    {
        // Simple schema generation for documentation
        // In production, consider using a proper schema generator
        var type = typeof(T);
        var properties = type.GetProperties()
            .Select(p => $"  \"{p.Name}\": <{p.PropertyType.Name}>")
            .ToList();
            
        return "{\n" + string.Join(",\n", properties) + "\n}";
    }
    
    private async Task<T?> GenerateStructuredFallbackAsync<T>(
        Kernel kernel,
        IChatCompletionService chatService,
        ChatHistory chatHistory,
        CancellationToken cancellationToken)
    {
        // Fallback implementation without JSON mode
        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);
            
        if (string.IsNullOrWhiteSpace(response.Content))
            return default;
            
        try
        {
            // Extract JSON from response even if it contains other text
            var jsonStart = response.Content.IndexOf('{');
            var jsonEnd = response.Content.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            
            // Try parsing as simple type
            if (IsSimpleType<T>())
            {
                return ParseSimpleType<T>(response.Content);
            }
            
            return default;
        }
        catch
        {
            return default;
        }
    }
}
```

## Success Criteria
- Both implementations compile and work with Semantic Kernel
- Reasoning model returns plain text responses
- Tooling model handles both simple types (bool, int, string) and complex JSON objects
- Null is returned when information cannot be extracted
- Implementations are thread-safe (new kernel per request)
- JSON parsing errors are handled gracefully

## Notes
- Consider caching kernels in the future for performance
- The structured output approach should work with multiple providers
- Error handling should distinguish between API errors and parsing errors
- Schema generation could be improved with a proper JSON Schema library