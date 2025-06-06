# Task 015.4: Progressive Complexity Tests

## Overview
Implement tests that progressively increase in difficulty to understand capability boundaries across different complexity levels.

## Goal
Document success rates across graduated difficulty levels to understand where models begin to struggle.

## Scope
1. **Level 1 - Direct Information**
   - Information explicitly stated in text
   - No inference required
   - Expected success: 95%+

2. **Level 2 - Simple Inference**
   - Basic calculations (e.g., overdue by X days)
   - Simple boolean determinations
   - Relationship inference
   - Expected success: 85%+

3. **Level 3 - Complex Reasoning**
   - Multiple related data points
   - Nested object extraction
   - Collection aggregation
   - Expected success: 70%+

4. **Level 4 - Ambiguous/Partial**
   - Missing information handling
   - Ambiguous references
   - Should return null appropriately
   - Expected success: 50%+ (null returns)

## Files to Create
```
tests/LangBridge.Tests/Integration/
└── TextContextualBridge/
    └── ProgressiveComplexityTests.cs
```

## Test Implementation Pattern
```csharp
[Theory, Trait("Category", "Progressive")]
[Trait("RequiresLLM", "true")]
[InlineData(ComplexityLevel.Direct, 0.95)]
[InlineData(ComplexityLevel.Inferred, 0.85)]
[InlineData(ComplexityLevel.Complex, 0.70)]
[InlineData(ComplexityLevel.Ambiguous, 0.50)]
public async Task ExtractAsync_ByComplexity_MeetsExpectedSuccessRate(
    ComplexityLevel level, 
    double expectedRate)
{
    // Run multiple attempts and calculate success rate
    var scenarios = TestDataSets.GetScenariosForLevel(level);
    var successRate = await ModelTestHelpers.CalculateSuccessRate(
        bridge, 
        scenarios, 
        attempts: 10);
    
    Assert.True(successRate >= expectedRate, 
        $"Level {level} success rate {successRate:P} below expected {expectedRate:P}");
}
```

## Dependencies
- Requires Task 015.2 for test infrastructure
- Requires Task 015.3 completion for baseline

## Success Criteria
- Clear documentation of success rates per level
- Reproducible test scenarios
- Model comparison insights

## Estimated Effort
~250-300 lines of test code