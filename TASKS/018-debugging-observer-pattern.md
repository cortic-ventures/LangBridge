# Task 018: Debugging Observer Pattern

## Overview
Implement an optional observer pattern for debugging that allows developers to register callbacks to capture prompts, responses, and extraction steps without polluting the main API.

## Problem Statement
- Developers have no visibility into LLM interactions during extraction
- Difficult to debug why certain extractions fail
- No way to trace the step-by-step extraction process
- Current approach is "blind" - developers can't see what prompts are sent or responses received

## Solution
Implement a lightweight observer pattern that captures key extraction events for debugging purposes.

## Observer Interface Design

### Core Observer Interface
```csharp
public interface IExtractionObserver
{
    /// <summary>
    /// Called when assessing if a property can be extracted from the input
    /// </summary>
    void OnPropertyAssessment(
        string propertyPath, 
        string prompt, 
        string response, 
        bool canExtract);
    
    /// <summary>
    /// Called when extracting the actual value of a property
    /// </summary>
    void OnPropertyExtraction(
        string propertyPath, 
        string prompt, 
        string response);
    
    /// <summary>
    /// Called when structuring extracted data into the target type
    /// </summary>
    void OnDataStructuring(
        string extractedData, 
        string jsonSchema, 
        string response,
        bool success);
    
    /// <summary>
    /// Called when the overall extraction process completes
    /// </summary>
    void OnExtractionComplete(
        Type targetType, 
        bool success, 
        string? errorMessage,
        TimeSpan duration);
}
```

### Optional Async Observer Interface
```csharp
public interface IAsyncExtractionObserver
{
    Task OnPropertyAssessmentAsync(
        string propertyPath, 
        string prompt, 
        string response, 
        bool canExtract,
        CancellationToken cancellationToken = default);
    
    Task OnPropertyExtractionAsync(
        string propertyPath, 
        string prompt, 
        string response,
        CancellationToken cancellationToken = default);
    
    Task OnDataStructuringAsync(
        string extractedData, 
        string jsonSchema, 
        string response,
        bool success,
        CancellationToken cancellationToken = default);
    
    Task OnExtractionCompleteAsync(
        Type targetType, 
        bool success, 
        string? errorMessage,
        TimeSpan duration,
        CancellationToken cancellationToken = default);
}
```

## Implementation Plan

### 1. Create Observer Infrastructure

**File: `src/LangBridge/Diagnostics/IExtractionObserver.cs`**
```csharp
namespace LangBridge.Diagnostics;

public interface IExtractionObserver
{
    // Interface definition as above
}

public interface IAsyncExtractionObserver
{
    // Async interface definition as above
}
```

**File: `src/LangBridge/Diagnostics/ExtractionObserverCollection.cs`**
```csharp
internal class ExtractionObserverCollection
{
    private readonly List<IExtractionObserver> _observers = new();
    private readonly List<IAsyncExtractionObserver> _asyncObservers = new();
    
    public void Add(IExtractionObserver observer) => _observers.Add(observer);
    public void Add(IAsyncExtractionObserver observer) => _asyncObservers.Add(observer);
    
    public void NotifyPropertyAssessment(string propertyPath, string prompt, string response, bool canExtract)
    {
        foreach (var observer in _observers)
        {
            try
            {
                observer.OnPropertyAssessment(propertyPath, prompt, response, canExtract);
            }
            catch
            {
                // Swallow observer exceptions to prevent disrupting extraction
            }
        }
    }
    
    public async Task NotifyPropertyAssessmentAsync(
        string propertyPath, 
        string prompt, 
        string response, 
        bool canExtract,
        CancellationToken cancellationToken)
    {
        var tasks = _asyncObservers.Select(async observer =>
        {
            try
            {
                await observer.OnPropertyAssessmentAsync(propertyPath, prompt, response, canExtract, cancellationToken);
            }
            catch
            {
                // Swallow observer exceptions
            }
        });
        
        await Task.WhenAll(tasks);
    }
    
    // Similar methods for other events...
}
```

### 2. Integrate Observers into TextContextualBridge

**Modify: `src/LangBridge/Internal/Infrastructure/ContextualBridging/TextContextualBridge.cs`**

Add observer collection as dependency:
```csharp
internal class TextContextualBridge : ITextContextualBridge
{
    private readonly IReasoningModel _reasoningModel;
    private readonly IDataStructuringModel _dataStructuringModel;
    private readonly ExtractionObserverCollection _observers;
    
    public TextContextualBridge(
        IReasoningModel reasoningModel,
        IDataStructuringModel dataStructuringModel,
        ExtractionObserverCollection observers)
    {
        _reasoningModel = reasoningModel ?? throw new ArgumentNullException(nameof(reasoningModel));
        _dataStructuringModel = dataStructuringModel ?? throw new ArgumentNullException(nameof(dataStructuringModel));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }
}
```

Add observer notifications throughout the extraction process:
```csharp
public async Task<Result<T>> TryFullExtractionAsync<T>(...)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // ... existing validation code ...
        
        var propertyNames = GetTypePropertyPaths<T>();
        
        // Property Assessment with Observer Notifications
        var assessmentTasks = propertyNames.Select(async propertyName => 
        {
            var prompt = $"Given this text block: <input_text_block>{input}</input_text_block>...";
            var response = await _reasoningModel.ReasonAsync(prompt, systemInstructions: "...", cancellationToken);
            var canExtract = response.StartsWith("yes", StringComparison.OrdinalIgnoreCase);
            
            // Notify observers
            _observers.NotifyPropertyAssessment(propertyName, prompt, response, canExtract);
            await _observers.NotifyPropertyAssessmentAsync(propertyName, prompt, response, canExtract, cancellationToken);
            
            return (propertyName, response, canExtract);
        });
        
        // ... rest of extraction logic with similar observer notifications ...
    }
    finally
    {
        stopwatch.Stop();
        _observers.NotifyExtractionComplete(typeof(T), success, errorMessage, stopwatch.Elapsed);
        await _observers.NotifyExtractionCompleteAsync(typeof(T), success, errorMessage, stopwatch.Elapsed, cancellationToken);
    }
}
```

### 3. Registration and Configuration

**Extend: `src/LangBridge/Extensions/ServiceCollectionExtensions.cs`**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLangBridge(this IServiceCollection services, LangBridgeOptions options)
    {
        // ... existing registration ...
        
        // Register observer collection as singleton
        services.AddSingleton<ExtractionObserverCollection>();
        
        return services;
    }
    
    public static IServiceCollection AddExtractionObserver<T>(this IServiceCollection services) 
        where T : class, IExtractionObserver
    {
        services.AddSingleton<IExtractionObserver, T>();
        
        // Automatically register with collection
        services.Configure<ServiceDescriptor>(provider =>
        {
            var collection = provider.GetRequiredService<ExtractionObserverCollection>();
            var observer = provider.GetRequiredService<IExtractionObserver>();
            collection.Add(observer);
        });
        
        return services;
    }
    
    public static IServiceCollection AddAsyncExtractionObserver<T>(this IServiceCollection services) 
        where T : class, IAsyncExtractionObserver
    {
        // Similar implementation for async observers
        return services;
    }
}
```

### 4. Built-in Observer Implementations

**File: `src/LangBridge/Diagnostics/ConsoleLoggingObserver.cs`**
```csharp
public class ConsoleLoggingObserver : IExtractionObserver
{
    public void OnPropertyAssessment(string propertyPath, string prompt, string response, bool canExtract)
    {
        Console.WriteLine($"[ASSESSMENT] {propertyPath}: {(canExtract ? "✓" : "✗")}");
        Console.WriteLine($"  Prompt: {prompt[..Math.Min(100, prompt.Length)]}...");
        Console.WriteLine($"  Response: {response[..Math.Min(100, response.Length)]}...");
        Console.WriteLine();
    }
    
    public void OnPropertyExtraction(string propertyPath, string prompt, string response)
    {
        Console.WriteLine($"[EXTRACTION] {propertyPath}");
        Console.WriteLine($"  Value: {response}");
        Console.WriteLine();
    }
    
    public void OnDataStructuring(string extractedData, string jsonSchema, string response, bool success)
    {
        Console.WriteLine($"[STRUCTURING] {(success ? "✓" : "✗")}");
        Console.WriteLine($"  Schema: {jsonSchema[..Math.Min(200, jsonSchema.Length)]}...");
        Console.WriteLine();
    }
    
    public void OnExtractionComplete(Type targetType, bool success, string? errorMessage, TimeSpan duration)
    {
        Console.WriteLine($"[COMPLETE] {targetType.Name}: {(success ? "✓" : "✗")} ({duration.TotalMilliseconds:F0}ms)");
        if (errorMessage != null)
        {
            Console.WriteLine($"  Error: {errorMessage}");
        }
        Console.WriteLine(new string('=', 50));
    }
}
```

**File: `src/LangBridge/Diagnostics/FileLoggingObserver.cs`**
```csharp
public class FileLoggingObserver : IAsyncExtractionObserver
{
    private readonly string _logPath;
    
    public FileLoggingObserver(string logPath = "langbridge-debug.log")
    {
        _logPath = logPath;
    }
    
    public async Task OnPropertyAssessmentAsync(string propertyPath, string prompt, string response, bool canExtract, CancellationToken cancellationToken = default)
    {
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ASSESSMENT {propertyPath}: {canExtract}\n" +
                      $"Prompt: {prompt}\n" +
                      $"Response: {response}\n\n";
        
        await File.AppendAllTextAsync(_logPath, logEntry, cancellationToken);
    }
    
    // Similar implementations for other methods...
}
```

### 5. Usage Examples

**Development Setup:**
```csharp
// In Program.cs or Startup.cs
services.AddLangBridge(options)
    .AddExtractionObserver<ConsoleLoggingObserver>()
    .AddAsyncExtractionObserver<FileLoggingObserver>();
```

**Custom Observer:**
```csharp
public class MetricsObserver : IExtractionObserver
{
    private readonly IMetricsCollector _metrics;
    
    public void OnExtractionComplete(Type targetType, bool success, string? errorMessage, TimeSpan duration)
    {
        _metrics.RecordExtractionDuration(targetType.Name, duration);
        _metrics.IncrementExtractionAttempts(targetType.Name, success);
    }
    
    // Other methods can be no-ops if only interested in completion metrics
    public void OnPropertyAssessment(string propertyPath, string prompt, string response, bool canExtract) { }
    public void OnPropertyExtraction(string propertyPath, string prompt, string response) { }
    public void OnDataStructuring(string extractedData, string jsonSchema, string response, bool success) { }
}
```

## Files to Create/Modify

### New Files:
1. `src/LangBridge/Diagnostics/IExtractionObserver.cs`
2. `src/LangBridge/Diagnostics/IAsyncExtractionObserver.cs`  
3. `src/LangBridge/Diagnostics/ExtractionObserverCollection.cs`
4. `src/LangBridge/Diagnostics/ConsoleLoggingObserver.cs`
5. `src/LangBridge/Diagnostics/FileLoggingObserver.cs`

### Modified Files:
1. `src/LangBridge/Internal/Infrastructure/ContextualBridging/TextContextualBridge.cs` - Add observer integration
2. `src/LangBridge/Extensions/ServiceCollectionExtensions.cs` - Add observer registration methods
3. `examples/LangBridge.Examples.Console/Program.cs` - Demonstrate observer usage

## Benefits

1. **Non-Intrusive**: Optional feature, doesn't affect main API
2. **Flexible**: Developers can create custom observers for their needs  
3. **Performance**: Observers are called synchronously but exceptions are swallowed
4. **Rich Debugging**: Full visibility into LLM interactions
5. **Production Ready**: Can be used for metrics collection in production
6. **Testable**: Easy to write tests that verify extraction behavior

## Success Criteria

1. Observer pattern integrated without affecting existing functionality
2. Both sync and async observer interfaces available
3. Built-in console and file logging observers provided
4. Easy registration via service collection extensions
5. Exception handling prevents observer failures from disrupting extraction
6. Example usage demonstrated in console application
7. Zero performance impact when no observers are registered

## Estimated Effort

- Observer interfaces and infrastructure: ~200 lines
- TextContextualBridge integration: ~100 lines
- Built-in observer implementations: ~150 lines  
- Service registration extensions: ~100 lines
- Examples and tests: ~100 lines
- Total: ~650 lines