# LangBridge 🌉

> **Status:** alpha (v0.1.0)

LangBridge is a C# library for extracting structured data from unstructured text using LLMs. 

**What it does:** Provides a simple API that returns either complete, type-safe objects or detailed error information. Built around an atomic extraction approach where operations either succeed entirely or fail with clear explanations.

**Why we built it:** Working with raw LLM APIs for data extraction involves a lot of repetitive work—prompt engineering, JSON parsing, error handling, type validation. LangBridge handles these concerns so you can focus on your business logic.

## Features

- **Atomic operations**: Extractions either succeed completely or fail with detailed explanations
- **Type safety**: Full compile-time checking and IntelliSense support
- **Simple API**: Single method handles prompt engineering, parsing, and error handling
- **Multi-provider**: Works with OpenAI, Azure OpenAI, Ollama, and other LLM providers
- **Production patterns**: Built-in Result<T> error handling, timeouts, and configuration management
- **Developer control**: Fine-tune extractions through Description attributes and custom queries

## Example

```csharp
// Extract structured data with a single call
var result = await contextualBridge.ExtractAsync<CustomerFeedback>(
    "The product keeps crashing and I've lost hours of work. This is unacceptable!",
    "Extract customer feedback details");

if (result.IsSuccess)
{
    var feedback = result.Value; // Complete CustomerFeedback object
    Console.WriteLine($"Sentiment: {feedback.Sentiment}");
    Console.WriteLine($"Severity: {feedback.SeverityScore}");
}
else
{
    Console.WriteLine($"Extraction failed: {result.Error}");
}
```

## Developer Control

LangBridge gives you precise control over extractions through two key mechanisms:

### 1. Description Attributes
Guide the LLM with specific instructions for each property:

```csharp
public record CustomerFeedback(
    [property: Description("Sentiment: Positive, Negative, or Neutral")]
    string Sentiment,
    
    [property: Description("Severity from 1 (minor) to 10 (critical)")]
    int SeverityScore,
    
    [property: Description("True if customer explicitly requests follow-up contact")]
    bool RequiresFollowUp);
```

### 2. Custom Queries
Tailor the extraction context for your specific use case:

```csharp
// General extraction
await contextualBridge.ExtractAsync<Customer>(text, "Extract customer information");

// Domain-specific extraction
await contextualBridge.ExtractAsync<Customer>(text, "Extract customer data from this support ticket");

// Context-aware extraction
await contextualBridge.ExtractAsync<Customer>(text, "Extract billing customer details for invoice processing");
```

**Why this matters**: You maintain full control over the extraction logic while LangBridge handles the technical complexity. No black-box magic—just clear, configurable instructions that you can tune for your domain.

## Quick Start

### Installation

```bash
dotnet add package LangBridge
```

### Basic Usage

```csharp
using LangcontextualBridge.ContextualBridging;
using LangcontextualBridge.Extensions;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;

// Configure services with IConfiguration
services.AddLangBridge(configuration);

var bridge = serviceProvider.GetRequiredService<ITextContextualBridge>();

// Extract complex business objects
public record CustomerFeedback(
    string Sentiment,
    string MainConcern,
    bool RequiresFollowUp,
    int SeverityScore);

var feedbackResult = await contextualBridge.ExtractAsync<CustomerFeedback>(
    "The product keeps crashing and I've lost hours of work. This is unacceptable!",
    "Extract customer feedback details");

if (feedbackResult.IsSuccess)
{
    var feedback = feedbackResult.Value;
    // All properties are guaranteed to be extracted or the entire operation fails
    Console.WriteLine($"Sentiment: {feedback.Sentiment}"); // "Negative"
    Console.WriteLine($"Severity: {feedback.SeverityScore}"); // 9
}

// Extract simple types
var cancellationResult = await contextualBridge.ExtractAsync<bool>(
    "I'm not happy with the service and want to cancel immediately.",
    "Did the user request cancellation?");

if (cancellationResult.IsSuccess)
{
    Console.WriteLine($"Cancelled: {cancellationResult.Value}"); // true
}
```

### Workflow Integration

LangBridge enables sophisticated multi-step workflows with chained extractions:

```csharp
using System.ComponentModel;

// Multi-Stage Customer Support Pipeline
public async Task ProcessSupportEmail(string emailContent)
{
    // Stage 1: Extract basic ticket information
    var ticketResult = await contextualBridge.ExtractAsync<SupportTicket>(
        emailContent, 
        "Extract support ticket information");
    
    if (ticketResult.IsFailure) 
    {
        await escalationService.HandleUnprocessableEmail(emailContent, ticketResult.Error);
        return;
    }
    
    var ticket = ticketResult.Value;
    
    // Stage 2: Analyze customer sentiment and urgency
    var sentimentResult = await contextualBridge.ExtractAsync<SentimentAnalysis>(
        emailContent,
        "Analyze customer sentiment, frustration level, and urgency indicators");
    
    // Stage 3: Extract technical details if it's a technical issue
    TechnicalDetails? techDetails = null;
    if (ticket.Category == "Technical")
    {
        var techResult = await contextualBridge.ExtractAsync<TechnicalDetails>(
            emailContent,
            "Extract technical problem details, error messages, and steps to reproduce");
            
        if (techResult.IsSuccess)
            techDetails = techResult.Value;
    }
    
    // Stage 4: Determine resolution strategy based on all extracted data
    var resolutionInput = $"""
        Ticket: {ticket.Subject}
        Category: {ticket.Category}
        Priority: {ticket.Priority}
        Customer Sentiment: {sentimentResult.Value?.SentimentScore ?? 0.5}
        Technical Issue: {techDetails?.ProblemType ?? "N/A"}
        """;
        
    var strategyResult = await contextualBridge.ExtractAsync<ResolutionStrategy>(
        resolutionInput,
        "Determine the best resolution approach and required resources");
    
    // Execute workflow based on chained extractions
    if (strategyResult.IsSuccess)
    {
        var strategy = strategyResult.Value;
        
        // Route with full context
        await routingService.RouteTicket(ticket, sentimentResult.Value, strategy);
        
        // Generate contextual response
        await responseService.GenerateResponse(ticket, techDetails, strategy);
        
        // Update CRM with comprehensive data
        await crmService.CreateEnrichedTicket(ticket, sentimentResult.Value, techDetails, strategy);
    }
}

public record SupportTicket(
    string CustomerEmail,
    string Subject,
    [property: Description("Category of the issue: Technical, Billing, or General")]
    string Category,
    [property: Description("Priority level from 1 (low) to 5 (critical)")]
    int Priority,
    [property: Description("List of products or services mentioned in the ticket")]
    List<string> ProductsAffected);

public record SentimentAnalysis(
    [property: Description("Primary emotional tone: Frustrated, Disappointed, Angry, Neutral, Satisfied")]
    string PrimarySentiment,
    [property: Description("Sentiment score from 0.0 (very negative) to 1.0 (very positive)")]
    double SentimentScore,
    [property: Description("Whether immediate escalation is needed based on language intensity")]
    bool IsEscalationRequired,
    [property: Description("Specific words or phrases indicating emotional state")]
    List<string> EmotionalIndicators);

public record TechnicalDetails(
    [property: Description("Type of technical problem: Bug, Performance, Integration, or User Error")]
    string ProblemType,
    [property: Description("Exact error message or code mentioned by the user")]
    string ErrorMessage,
    [property: Description("Steps the user took that led to the problem")]
    List<string> StepsToReproduce,
    [property: Description("Specific feature or component affected")]
    string AffectedFeature,
    [property: Description("User's technical environment: browser, OS, version details")]
    string UserEnvironment);

public record ResolutionStrategy(
    [property: Description("Recommended next action: Immediate Fix, Escalate to Engineering, Send Documentation")]
    string RecommendedAction,
    [property: Description("Estimated time to resolve in hours")]
    int EstimatedResolutionTime,
    [property: Description("Team members or resources needed for resolution")]
    List<string> RequiredResources,
    [property: Description("Appropriate response tone: Apologetic, Professional, Technical")]
    string ResponseTone);
```

### Error Handling

```csharp
var result = await contextualBridge.ExtractAsync<Order>(orderEmail, "Extract order details");

result.Match(
    onSuccess: order => ProcessOrder(order),
    onFailure: error => _logger.LogWarning("Extraction failed: {Error}", error)
);

// Or handle failures explicitly
if (result.IsFailure)
{
    Console.WriteLine($"Failed to extract order: {result.Error}");
    // Handle the failure case - maybe retry, use defaults, or alert user
}
```

## Architecture

LangBridge uses a clean architecture with clear separation between public API and internal implementation:

- **Public API**: Simple, focused interfaces for text extraction
- **Internal Infrastructure**: Encapsulated LLM interactions and processing logic
- **Type System**: Advanced reflection utilities for deep property analysis
- **Result Pattern**: Functional error handling without exceptions

## Configuration

Add to your `appsettings.json`:

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

**Supported Providers**: OpenAI, Azure OpenAI, Ollama (local), Groq, OpenRouter. See [examples/](examples/) for provider-specific configurations.

**Coming Soon**: Multiple bridge configurations (fast vs powerful models) with keyed services support.

## Advanced Features

### Complex Document Processing

LangBridge excels at extracting sophisticated business data from unstructured documents:

```csharp
using System.ComponentModel;

public record ContractAnalysis(
    [property: Description("Type of contract: Service Agreement, License, NDA, Employment, etc.")]
    string ContractType,
    [property: Description("All parties involved in the contract")]
    List<Party> Parties,
    DateTime EffectiveDate,
    [property: Description("Contract expiration date if specified, null if perpetual")]
    DateTime? ExpirationDate,
    [property: Description("Total contract value in dollars if mentioned")]
    decimal? TotalValue,
    [property: Description("Important clauses, terms, or conditions identified")]
    List<string> KeyTerms,
    [property: Description("Payment-related terms as key-value pairs, e.g. 'DueDate': 'Net 30'")]
    Dictionary<string, string> PaymentTerms,
    [property: Description("Risk assessment score from 1 (low risk) to 10 (high risk)")]
    int RiskScore);

public record Party(
    string Name,
    [property: Description("Role in contract: Client, Vendor, Guarantor, Witness")]
    string Role,
    string Address,
    [property: Description("Contact email address if provided")]
    string ContactEmail);

// Extract from complex legal document
var contractText = File.ReadAllText("service-agreement.txt");
var analysisResult = await contextualBridge.ExtractAsync<ContractAnalysis>(
    contractText, 
    "Analyze this contract and extract key business terms and parties");

if (analysisResult.IsSuccess)
{
    var contract = analysisResult.Value;
    // All nested objects, collections, and calculated fields extracted reliably
    await contractRepository.Save(contract);
    await complianceService.ReviewContract(contract);
}
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
- ✅ Core `ExtractAsync<T>()` API with configurable extraction modes
- ✅ Support for simple and complex types with deep property extraction
- ✅ Atomic operations using `Result<T>` pattern with detailed error reporting
- ✅ Multi-provider LLM support (OpenAI, Ollama, Azure OpenAI, Groq, OpenRouter)
- ✅ TypeSystem with advanced reflection utilities and circular reference detection
- ✅ Comprehensive testing framework (185 tests including complex showcase scenarios)
- ✅ Production-ready architecture with clean separation of concerns

## Roadmap

### v0.1.0 (Current - Ready for Release)
- ✅ Core `ExtractAsync<T>()` API with configurable extraction modes
- ✅ Support for simple and complex types with deep property extraction
- ✅ Atomic operations using `Result<T>` pattern with detailed error reporting
- ✅ Multi-provider LLM support (OpenAI, Ollama, Azure OpenAI, Groq, OpenRouter)
- ✅ TypeSystem with advanced reflection utilities and circular reference detection
- ✅ Comprehensive testing framework (185 tests including complex showcase scenarios)
- ✅ Production-ready architecture with clean separation of concerns
- [ ] NuGet package publication

### v0.2.0 (Next Release)
- [ ] Complete API documentation and production examples
- [ ] Additional extraction modes (BestEffort, RequiredOnly)
- [ ] Keyed services support for multiple bridge configurations
- [ ] Debugging observer pattern for development workflows

### v1.0.0 (Production Ready)
- [ ] Stable API with backward compatibility guarantee
- [ ] Advanced features: batch processing, debugging observers
- [ ] Enterprise features: monitoring, caching, and performance optimizations

**Use cases**: Customer support analysis, document processing, data entry automation, content moderation, and AI agent development.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT — see [LICENSE](LICENSE) file.
