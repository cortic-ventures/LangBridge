# Task 015.3: High-Confidence Extraction Tests

## Overview
Implement integration tests for scenarios where LLM success is highly probable (95%+ success rate expected).

## Goal
Validate basic integration with real LLM models using simple, clear extraction scenarios.

## Scope
1. **Simple Type Extractions**
   - Names from clear text
   - Ages and numbers
   - Boolean values
   - Dates with clear formats
   - Simple strings

2. **Well-Structured Data**
   - Contact information cards
   - Simple invoices
   - Product descriptions
   - Basic order information
   - Clear address data

3. **Direct Information Tests**
   ```csharp
   // Example scenarios:
   "John Smith is 30 years old" → { Name: "John Smith", Age: 30 }
   "The product costs $19.99" → { Price: 19.99 }
   "Meeting scheduled for March 15, 2024" → { Date: DateTime }
   ```

## Files to Create
```
tests/LangBridge.Tests/Integration/
└── TextContextualBridge/
    └── HighConfidenceExtractionTests.cs
```

## Test Implementation Pattern
```csharp
[Fact, Trait("Category", "HighConfidence")]
[Trait("RequiresLLM", "true")]
public async Task ExtractAsync_SimplePersonInfo_ExtractsCorrectly()
{
    // Arrange
    var bridge = CreateRealBridge(); // Uses actual LLM
    var text = TestDataSets.SimpleExtractions.PersonInfo;
    
    // Act
    var result = await bridge.ExtractAsync<Person>(text);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("John Smith", result.Name);
    Assert.Equal(30, result.Age);
}
```

## Dependencies
- Requires Task 015.2 for test infrastructure
- Requires actual LLM configuration

## Success Criteria
- 95%+ success rate across all high-confidence tests
- Tests can run against multiple model providers
- Clear documentation of any model-specific behaviors

## Estimated Effort
~200-250 lines of test code