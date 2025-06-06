# Task 015.5: Resilience and Error Handling Tests

## Overview
Test graceful handling of model limitations, errors, and edge cases to ensure robust behavior under adverse conditions.

## Goal
Verify the system handles all failure modes gracefully without throwing unhandled exceptions.

## Scope
1. **Model Response Errors**
   - Invalid JSON from data structuring model
   - Empty/null responses
   - Partial JSON responses
   - Malformed schema responses

2. **Timeout and Cancellation**
   - Cancellation token behavior
   - Timeout scenarios
   - Long-running extractions

3. **Model Limitations**
   - Rate limiting responses
   - Model refusals
   - Token limit exceeded
   - API errors

4. **Edge Case Inputs**
   - Empty text
   - Very long text
   - Special characters/encoding
   - Non-English text
   - Binary/encoded data

## Files to Create
```
tests/LangBridge.Tests/Integration/
└── TextContextualBridge/
    └── ResilienceAndErrorHandlingTests.cs
```

## Test Implementation Pattern
```csharp
[Fact, Trait("Category", "Resilience")]
public async Task ExtractAsync_WithMalformedResponse_ReturnsNull()
{
    // Arrange - Mock model to return invalid JSON
    var mockDataModel = new Mock<IDataStructuringModel>();
    mockDataModel.Setup(m => m.GenerateStructuredDataAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync("{invalid json");
    
    var bridge = CreateBridgeWithMock(mockDataModel.Object);
    
    // Act
    var result = await bridge.ExtractAsync<Person>("valid text");
    
    // Assert
    Assert.Null(result); // Should handle gracefully
}
```

## Dependencies
- Requires Task 015.1 for mock patterns
- Requires Task 015.2 for test infrastructure

## Success Criteria
- Zero unhandled exceptions
- Proper null returns for all failure modes
- Clear error patterns documented
- Graceful degradation demonstrated

## Estimated Effort
~200-250 lines of test code