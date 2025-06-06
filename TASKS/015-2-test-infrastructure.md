# Task 015.2: Test Infrastructure and Helpers

## Overview
Create shared test infrastructure and helper utilities for integration testing, including test data sets, model helpers, and custom assertions.

## Goal
Establish reusable components that all integration tests can leverage for consistent testing patterns.

## Scope
1. **Test Data Management**
   ```csharp
   // TestDataSets.cs
   - Simple extraction scenarios
   - Complex object scenarios
   - Edge case inputs
   - Expected outputs for each scenario
   ```

2. **Model Test Helpers**
   ```csharp
   // ModelTestHelpers.cs
   - TextContextualBridge factory methods
   - Success rate calculation utilities
   - Multiple attempt runners
   - Model configuration helpers
   ```

3. **Custom Assertions**
   ```csharp
   // AssertionExtensions.cs
   - AI-specific assertions
   - Confidence-based comparisons
   - Partial match validations
   - Graceful failure assertions
   ```

4. **Configuration Support**
   - Test settings for API keys
   - Model selection for tests
   - Environment-based configuration
   - Timeout configurations

## Files to Create
```
tests/LangBridge.Tests/Integration/
└── Shared/
    ├── TestDataSets.cs
    ├── ModelTestHelpers.cs
    ├── AssertionExtensions.cs
    └── TestConfiguration.cs
```

## Dependencies
- Requires Task 015.1 completion for integration patterns

## Success Criteria
- Reusable components for all integration tests
- Clear separation of concerns
- Easy to extend with new test scenarios

## Estimated Effort
~300-400 lines of infrastructure code