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
using LangBridge.Extensions;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;

// Configure services with IConfiguration
services.AddLangBridge(configuration);

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
    "Models": [
      {
        "Purpose": "Reasoning",
        "Provider": "OpenAI",
        "ModelName": "gpt-4o",
        "ApiKey": "your-openai-api-key",
        "Endpoint": "https://api.openai.com/v1"
      },
      {
        "Purpose": "Tooling",
        "Provider": "OpenAI",
        "ModelName": "gpt-4o-mini",
        "ApiKey": "your-openai-api-key",
        "Endpoint": "https://api.openai.com/v1"
      }
    ]
  }
}
```

### Additional Provider Examples

#### Ollama (Local)
```json
{
  "LangBridge": {
    "Models": [
      {
        "Purpose": "Reasoning",
        "Provider": "Ollama",
        "ModelName": "llama3.2:3b",
        "ApiKey": "test",
        "Endpoint": "http://localhost:11434"
      },
      {
        "Purpose": "Tooling",
        "Provider": "Ollama",
        "ModelName": "llama3.2:3b",
        "ApiKey": "test",
        "Endpoint": "http://localhost:11434"
      }
    ]
  }
}
```

#### Azure OpenAI
```json
{
  "LangBridge": {
    "Models": [
      {
        "Purpose": "Reasoning",
        "Provider": "AzureOpenAI",
        "ModelName": "gpt-4o",
        "ApiKey": "your-azure-api-key",
        "Endpoint": "https://your-resource.openai.azure.com"
      }
    ]
  }
}
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

## Current Status

### v0.0.1 (Pre-Alpha)
- ✅ Core extraction API with `Result<T>` pattern
- ✅ Support for simple and complex types  
- ✅ Deep property extraction with nested objects and collections
- ✅ Multi-provider LLM support (OpenAI, Ollama, Azure OpenAI, Groq, OpenRouter)
- ✅ Comprehensive error handling and atomic operations
- ✅ TypeSystem with advanced reflection utilities
- ✅ Integration testing framework with high-confidence test scenarios
- ✅ Circular reference detection and prevention

## Roadmap

### v0.1.0 (Next Release)
- [ ] **Test Project Cleanup**: Refactor and expand integration testing framework
  - Progressive complexity tests for graduated difficulty levels
  - Resilience and error handling tests for edge cases and failure modes
  - Multi-model comparison tests for cross-model performance analysis
  - Test infrastructure improvements and code cleanup
- [ ] **Package Publication**: Publish to NuGet with stable API
- [ ] **Documentation**: Complete API documentation and usage guides
- [ ] **Performance Benchmarks**: Published performance characteristics
- [ ] **Production Examples**: Real-world usage examples and best practices

### v0.2.0 (Future)
- [ ] **Partial Extraction**: Extract specific properties from complex types
- [ ] **Batch Processing**: Process multiple extractions efficiently  
- [ ] **Debugging Observer Pattern**: Subscribe to extraction events for debugging
- [ ] **Performance Optimizations**: Caching and parallel processing

### v1.0.0 (Stable Release)
- [ ] **Stable API**: Backward compatibility guarantee
- [ ] **Production Hardening**: Battle-tested in real applications
- [ ] **Advanced Features**: Custom type handlers, streaming support, extraction hints
- [ ] **Enterprise Features**: Advanced error handling, monitoring, and diagnostics

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT — see [LICENSE](LICENSE) file.
