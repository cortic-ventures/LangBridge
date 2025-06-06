# Task 015: Integration Testing Strategy for AI-Dependent Functionality

## Overview
Create comprehensive integration tests for `TextContextualBridge.ExtractAsync<T>()` that balance testing our code logic against the inherent variability of LLM responses.

## Challenge
Testing AI-dependent functionality presents unique challenges:
- **Code vs Model**: Our implementation may be correct, but the LLM model may fail to extract information
- **Non-deterministic responses**: Same input may yield different outputs across runs
- **Model capability variance**: Different models have different strengths/weaknesses
- **Context sensitivity**: Extraction success depends heavily on input text quality and structure

## Testing Strategy

### 1. Deterministic Foundation Tests
**Goal**: Verify our code logic works with known-good LLM responses
- Mock `IReasoningModel` and `IDataStructuringModel` with predictable responses
- Test JSON schema generation, deserialization, error handling
- Verify circular reference detection, type mapping, property path extraction
- Ensure consistent behavior regardless of model responses

### 2. High-Confidence Scenarios
**Goal**: Test with inputs where LLM success is highly probable
- **Simple extractions**: Extract name, age, boolean flags from clear text
- **Structured data**: Process well-formatted invoices, contact cards, product descriptions
- **Explicit information**: Text that directly states the required data points
- **Expected success rate**: 95%+ across multiple models

### 3. Progressive Complexity Tests
**Goal**: Understand capability boundaries across difficulty levels

**Level 1 - Direct Information**:
```csharp
// Text: "John Smith is 30 years old and lives in New York"
// Extract: { Name: "John Smith", Age: 30, City: "New York" }
```

**Level 2 - Inferred Information**:
```csharp
// Text: "The invoice dated March 15th is overdue by 30 days"
// Extract: { InvoiceDate: DateTime, IsOverdue: true, DaysOverdue: 30 }
```

**Level 3 - Complex Reasoning**:
```csharp
// Text: "We received 3 complaints about the blue widgets last month"
// Extract: { ProductIssues: [{ Product: "blue widgets", ComplaintCount: 3, Period: "last month" }] }
```

**Level 4 - Ambiguous/Missing Information**:
```csharp
// Text: "The meeting went well"
// Extract: { MeetingOutcome: "positive" } // Should return null for missing specific data
```

### 4. Model-Agnostic Resilience Tests
**Goal**: Verify graceful handling of model limitations
- **Partial extraction**: When some but not all properties can be determined
- **Invalid JSON responses**: Test fallback parsing mechanisms
- **Model refusals**: Handle cases where model refuses to process input
- **Timeout scenarios**: Test cancellation token behavior
- **Rate limiting**: Verify proper error handling for API limits

### 5. Multi-Model Comparison Tests
**Goal**: Understand relative model performance (optional, for CI insights)
- Same test scenarios across different models (GPT-4, Claude, etc.)
- Track success rates by model and complexity level
- Identify model-specific strengths/weaknesses
- Generate insights for model selection recommendations

## Test Implementation Approach

### 1. Test Categories
```csharp
[Collection("Integration")]
public class TextContextualBridgeIntegrationTests
{
    [Fact, Trait("Category", "Deterministic")]
    public async Task ExtractAsync_WithMockedModels_ReturnsExpectedResult()
    
    [Fact, Trait("Category", "HighConfidence")]
    public async Task ExtractAsync_SimplePersonInfo_ExtractsCorrectly()
    
    [Theory, Trait("Category", "Progressive")]
    [InlineData(ComplexityLevel.Direct)]
    [InlineData(ComplexityLevel.Inferred)]
    public async Task ExtractAsync_ByComplexity_MeetsExpectedSuccessRate()
    
    [Fact, Trait("Category", "Resilience")]
    public async Task ExtractAsync_WithMalformedResponse_HandlesGracefully()
}
```

### 2. Success Criteria Framework
- **Deterministic tests**: 100% success rate (tests our code, not models)
- **High-confidence tests**: 95%+ success rate (validates basic model integration)
- **Progressive tests**: Document success rates by complexity level
- **Resilience tests**: Verify no exceptions, proper null returns for failures

### 3. Test Data Strategy
- **Curated datasets**: Hand-crafted inputs with known expected outputs
- **Real-world samples**: Actual invoices, emails, documents (anonymized)
- **Edge cases**: Malformed text, missing information, ambiguous content
- **Negative cases**: Inputs that should deliberately return null

### 4. Assertion Strategy
```csharp
// For high-confidence scenarios
Assert.NotNull(result);
Assert.Equal(expectedValue, result.PropertyName);

// For progressive complexity (probabilistic)
var successRate = await RunMultipleAttempts(scenario, attemptCount: 10);
Assert.True(successRate >= expectedMinimumRate);

// For resilience (no exceptions, graceful degradation)
var result = await bridge.ExtractAsync<T>(malformedInput);
Assert.Null(result); // or partial result with available data
```

## Configuration Requirements
- Support for multiple model configurations in test settings
- Environment variables for API keys (CI/local development)
- Timeout configurations for long-running integration tests
- Test categorization for selective execution (unit vs integration vs performance)

## Success Metrics
1. **Code reliability**: 100% success on deterministic tests
2. **Basic integration**: 95%+ success on high-confidence scenarios
3. **Complexity handling**: Documented success rates across levels
4. **Error resilience**: Zero unhandled exceptions across all scenarios
5. **Performance baseline**: Establish response time expectations

## File Organization Strategy

### Recommended File Structure
```
tests/LangBridge.Tests/Integration/
├── TextContextualBridge/
│   ├── DeterministicTests.cs              # Mock-based, 100% predictable
│   ├── HighConfidenceExtractionTests.cs   # Simple, high-success scenarios
│   ├── ProgressiveComplexityTests.cs      # Graduated difficulty levels
│   ├── ResilienceAndErrorHandlingTests.cs # Edge cases, malformed input
│   └── MultiModelComparisonTests.cs       # Optional cross-model analysis
└── Shared/
    ├── TestDataSets.cs                     # Centralized test data
    ├── ModelTestHelpers.cs                 # Common test utilities
    └── AssertionExtensions.cs              # Custom assertions for AI testing
```

### File Size Guidelines
- **Target**: 100-200 lines per test file (5-15 test methods)
- **Maximum**: 300 lines before considering split
- **Split triggers**:
  - More than 15 test methods in one file
  - Multiple distinct testing purposes in same file
  - File becoming hard to navigate/understand

### File Splitting Criteria

**By Test Purpose**:
- `DeterministicTests.cs` - Only mocked model responses
- `HighConfidenceExtractionTests.cs` - Simple, predictable extractions
- `ProgressiveComplexityTests.cs` - Graduated difficulty scenarios
- `ResilienceAndErrorHandlingTests.cs` - Error conditions and edge cases

**By Data Type** (if files get large):
- `SimpleTypeExtractionTests.cs` - string, int, bool, DateTime
- `ComplexObjectExtractionTests.cs` - nested objects, collections
- `SpecializedTypeExtractionTests.cs` - enums, nullable types, custom types

**By Scenario Domain** (alternative approach):
- `DocumentProcessingTests.cs` - invoices, contracts, reports
- `ContactInformationTests.cs` - names, addresses, phone numbers  
- `BusinessDataTests.cs` - orders, products, transactions

### Shared Components Strategy
```csharp
// TestDataSets.cs - Centralized test data
public static class TestDataSets
{
    public static class SimpleExtractions
    {
        public const string PersonInfo = "John Smith is 30 years old...";
        public const string InvoiceData = "Invoice #12345 dated March 15th...";
    }
    
    public static class ComplexScenarios
    {
        public const string MultiplePeople = "The team includes Alice (manager), Bob (developer)...";
    }
}

// ModelTestHelpers.cs - Common utilities
public static class ModelTestHelpers  
{
    public static async Task<double> CalculateSuccessRate<T>(...)
    public static TextContextualBridge CreateTestBridge(...)
}

// AssertionExtensions.cs - AI-specific assertions
public static class AssertionExtensions
{
    public static void ShouldExtractWithConfidence<T>(this T result, T expected, double confidence = 0.95)
    public static void ShouldHandleGracefully<T>(this Task<T> extraction)
}
```

## Implementation Priority
1. **Phase 1**: Deterministic and high-confidence tests
2. **Phase 2**: Progressive complexity tests with success rate tracking  
3. **Phase 3**: Resilience and edge case testing
4. **Phase 4**: Multi-model comparison framework (optional)

## Notes
- Integration tests should be separate from unit tests (different test categories)
- Consider CI pipeline impact (cost, time) for LLM-dependent tests
- Document model-specific behaviors and limitations discovered during testing
- Maintain test data repository for consistent cross-run comparisons