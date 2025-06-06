# Task 015.6: Multi-Model Comparison Tests (Optional)

## Overview
Create optional tests that compare extraction performance across different LLM providers to generate insights for model selection.

## Goal
Understand relative model performance and provide data-driven recommendations for model selection.

## Scope
1. **Model Coverage**
   - OpenAI (GPT-4, GPT-3.5)
   - Anthropic (Claude)
   - Local models (Ollama)
   - Other providers (Groq, OpenRouter)

2. **Comparison Metrics**
   - Success rates by complexity level
   - Response time performance
   - Cost per extraction
   - Token usage efficiency

3. **Test Scenarios**
   - Same test data across all models
   - Various complexity levels
   - Different data types
   - Multiple languages (if applicable)

## Files to Create
```
tests/LangBridge.Tests/Integration/
└── TextContextualBridge/
    └── MultiModelComparisonTests.cs
```

## Implementation Pattern
```csharp
[Theory, Trait("Category", "ModelComparison")]
[Trait("RequiresMultipleModels", "true")]
[InlineData("openai/gpt-4")]
[InlineData("anthropic/claude-3")]
[InlineData("ollama/llama2")]
public async Task CompareModels_ForComplexityLevels_GeneratesReport(string modelId)
{
    // Run standardized test suite
    var results = await ModelTestHelpers.RunStandardizedSuite(modelId);
    
    // Generate comparison report
    var report = ModelTestHelpers.GenerateComparisonReport(results);
    
    // Store for later analysis
    await TestReportStorage.Save(modelId, report);
}
```

## Dependencies
- Requires all previous tasks (015.1-015.5)
- Requires multiple model configurations
- Optional - can be skipped initially

## Success Criteria
- Reproducible comparison data
- Clear model performance insights
- Cost/benefit analysis per model
- Recommendations for model selection

## Notes
- This is optional and can be implemented later
- Useful for CI/CD insights
- Can guide production model selection

## Estimated Effort
~150-200 lines of test code