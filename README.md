# LangBridge 🌉

> **Status:** pre-alpha (v0.0.1)

LangBridge is a C# library that provides atomic, reliable operations for extracting structured insights from unstructured text, designed specifically for building robust AI agents and workflows.

## Key Features

- **Unified API**: Single `TryFullExtractionAsync<T>()` method for all extraction needs
- **Type-Safe**: Extract both simple types (`bool`, `int`, `string`) and complex objects with full IntelliSense support
- **Atomic Operations**: All-or-nothing extraction with detailed error reporting via `Result<T>` pattern
- **Deep Property Extraction**: Automatically traverses nested objects and collections
- **Two-Model Architecture**: 
  - Reasoning models for complex analysis and information extraction
  - Data structuring models for reliable JSON output generation
- **Multi-Provider Support**: OpenAI, Anthropic, Azure OpenAI, and more
- **Production-Ready**: Built on Microsoft Semantic Kernel with robust error handling

## Quick Start

### Installation

```bash
dotnet add package LangBridge
```

### Basic Usage

```csharp
using LangBridge.ContextualBridging;
using CSharpFunctionalExtensions;

// Configure services
services.AddLangBridge(options =>
{
    options.ReasoningModel = new ModelConfig
    {
        Provider = AiProvider.OpenAI,
        ModelId = "gpt-4o",
        ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
    };
    options.ToolingModel = new ModelConfig
    {
        Provider = AiProvider.OpenAI,
        ModelId = "gpt-4o-mini",
        ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
    };
});

// Extract simple types
var bridge = serviceProvider.GetRequiredService<ITextContextualBridge>();

var result = await bridge.TryFullExtractionAsync<bool>(
    "I'm not happy with the service and want to cancel immediately.",
    "Did the user cancel their subscription?");

if (result.IsSuccess)
{
    Console.WriteLine($"Cancelled: {result.Value}"); // true
}

// Extract complex objects
public record CustomerFeedback(
    string Sentiment,
    string MainConcern,
    bool RequiresFollowUp,
    int SeverityScore);

var feedbackResult = await bridge.TryFullExtractionAsync<CustomerFeedback>(
    "The product keeps crashing and I've lost hours of work. This is unacceptable!",
    "Extract customer feedback details");

if (feedbackResult.IsSuccess)
{
    var feedback = feedbackResult.Value;
    // All properties are guaranteed to be extracted or the entire operation fails
}
```

### Handling Extraction Failures

```csharp
var result = await bridge.TryFullExtractionAsync<Order>(orderEmail, "Extract order details");

result.Match(
    onSuccess: order => ProcessOrder(order),
    onFailure: error => _logger.LogWarning("Extraction failed: {Error}", error)
);
```

## Architecture

LangBridge uses a clean architecture with clear separation between public API and internal implementation:

- **Public API**: Simple, focused interfaces for text extraction
- **Internal Infrastructure**: Encapsulated LLM interactions and processing logic
- **Type System**: Advanced reflection utilities for deep property analysis
- **Result Pattern**: Functional error handling without exceptions

## Configuration

### Via appsettings.json

```json
{
  "LangBridge": {
    "ReasoningModel": {
      "Provider": "OpenAI",
      "ModelId": "gpt-4o",
      "ApiKey": "${OPENAI_API_KEY}"
    },
    "ToolingModel": {
      "Provider": "OpenAI",  
      "ModelId": "gpt-4o-mini",
      "ApiKey": "${OPENAI_API_KEY}"
    }
  }
}
```

### Via Code

```csharp
services.AddLangBridge(options =>
{
    options.ReasoningModel = new ModelConfig
    {
        Provider = AiProvider.Anthropic,
        ModelId = "claude-3-5-sonnet-20241022",
        ApiKey = apiKey
    };
    // Configure other options...
});
```

## Advanced Features

### Deep Property Extraction

LangBridge automatically handles complex nested structures:

```csharp
public record Invoice(
    string InvoiceNumber,
    Customer Customer,
    List<LineItem> Items,
    decimal TotalAmount);

public record Customer(string Name, Address BillingAddress);
public record Address(string Street, string City, string PostalCode);
public record LineItem(string Description, int Quantity, decimal UnitPrice);

// LangBridge will extract all nested properties automatically
var invoiceResult = await bridge.TryFullExtractionAsync<Invoice>(emailText, "Extract invoice details");
```

### Circular Reference Detection

The library automatically detects and handles circular references in your types:

```csharp
// This will throw a clear exception at extraction time
public class Node 
{
    public string Value { get; set; }
    public Node Next { get; set; } // Circular reference!
}
```

## Roadmap

### v0.1.0 (Current Release)
- ✅ Core extraction API with `Result<T>` pattern
- ✅ Support for simple and complex types
- ✅ Deep property extraction
- ✅ Multi-provider LLM support
- ✅ Comprehensive error handling

### v0.2.0 (Next Release)
- [ ] **Partial Extraction**: Extract specific properties from complex types
  ```csharp
  // Extract only what you need
  var name = await bridge.TryPropertyExtractionAsync<Person, string>(
      text, 
      p => p.Name, 
      "Extract person's name");
  ```
- [ ] **Batch Processing**: Process multiple extractions efficiently
- [ ] **Extraction Templates**: Reusable extraction patterns
- [ ] **Performance Optimizations**: Caching and parallel processing

### v0.3.0 (Future)
- [ ] **Debugging Observer Pattern**: Subscribe to extraction events for debugging
  ```csharp
  eventBus.Subscribe<ExtractionCompletedEvent>(e => 
  {
      _logger.LogDebug("Extraction took {Duration}ms", e.Duration.TotalMilliseconds);
  });
  ```
- [ ] **Extraction Hints**: Guide LLM with additional context
- [ ] **Custom Type Handlers**: Register custom extraction logic for specific types
- [ ] **Streaming Support**: Process large documents incrementally

### v1.0.0 (Stable Release)
- [ ] **Stable API**: Backward compatibility guarantee
- [ ] **Production Hardening**: Battle-tested in real applications
- [ ] **Comprehensive Documentation**: Full API docs and best practices
- [ ] **Performance Benchmarks**: Published performance characteristics

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT — see [LICENSE](LICENSE) file.
