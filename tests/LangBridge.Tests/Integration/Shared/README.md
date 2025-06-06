# Test Infrastructure and Helpers

This directory contains shared test infrastructure and helper utilities for integration testing, implementing **Task 015.2** of the LangBridge integration testing strategy.

## Overview

The test infrastructure provides reusable components that all integration tests can leverage for consistent testing patterns, especially for AI-dependent functionality.

## Components

### 1. TestDataSets.cs
**Purpose**: Predefined test scenarios with expected inputs and outputs

**Features**:
- Simple type extraction scenarios (string, int, bool, decimal, DateTime)
- Complex type extraction scenarios (Person, Product, Address)
- Edge case scenarios (missing information, ambiguous data)
- Strongly-typed test data models with proper equality comparisons
- Configurable success/failure expectations

**Usage**:
```csharp
var scenario = TestDataSets.SimpleTypes.PersonName;
var complexScenario = TestDataSets.ComplexTypes.PersonExtraction;
var edgeCase = TestDataSets.EdgeCases.MissingInformation;
```

### 2. ModelTestHelpers.cs
**Purpose**: Factory methods and utilities for creating TextContextualBridge instances

**Features**:
- Deterministic bridge creation with mock models
- Pre-configured success/failure scenarios
- Multiple attempt runners for statistical analysis
- Property-level failure simulation
- Performance timing and metrics collection

**Key Methods**:
- `CreateDeterministicBridge()` - Basic bridge with mocks
- `CreateSuccessfulBridge<T>()` - Pre-configured for success
- `CreateFailingBridge()` - Pre-configured for failure scenarios
- `RunMultipleAttemptsAsync()` - Statistical testing
- `RunSingleAttemptAsync()` - Single execution with timing

### 3. AssertionExtensions.cs
**Purpose**: Custom assertion methods for AI-specific testing scenarios

**Features**:
- Result<T> pattern assertions (`ShouldBeSuccess`, `ShouldBeFailure`)
- Statistical assertions for multiple attempts
- Performance timing assertions
- Deterministic behavior validation
- Confidence threshold validation
- Partial matching for complex objects

**Key Assertions**:
- `ShouldMatchExpectedOutcome()` - Validates against scenario expectations
- `ShouldHaveMinimumSuccessRate()` - Statistical success validation
- `ShouldBeDeterministic()` - Consistency checking
- `ShouldCompleteWithinTime()` - Performance assertions

### 4. TestConfiguration.cs
**Purpose**: Configuration management for test execution and model settings

**Features**:
- Environment-based configuration support
- API key management for different providers
- Test scenario category settings
- Timeout and retry configuration
- Conditional test execution (skip if not configured)

**Configuration Sources**:
- `appsettings.test.json` - Test-specific settings
- Environment variables (prefixed with `LANGBRIDGE_TEST_`)
- Configuration binding for LangBridge options

## Integration with Existing Infrastructure

### Base Classes
The infrastructure builds upon existing base classes:
- **IntegrationTestBase**: Provides DI setup and mock management
- **MockReasoningModel** / **MockDataStructuringModel**: Deterministic model implementations

### Extension Points
- **TestScenario<T>**: Generic scenario definition
- **TestResults<T>**: Multi-attempt execution results
- **SingleTestResult<T>**: Single execution results

## Configuration Example

```json
{
  "TestSettings": {
    "RunAiDependentTests": false,
    "ModelTimeoutSeconds": 30,
    "ConfidenceThreshold": 0.8,
    "StatisticalTestAttempts": 5
  },
  "TestScenarios": {
    "HighConfidence": {
      "Enabled": true,
      "ExpectedSuccessRate": 0.95
    }
  },
  "ApiKeys": {
    "OpenAI": "",
    "Anthropic": ""
  }
}
```

## Usage Patterns

### Basic Test Structure
```csharp
[Fact]
public async Task TestName()
{
    // Arrange
    var scenario = TestDataSets.SimpleTypes.PersonName;
    var bridge = ModelTestHelpers.CreateSuccessfulBridge(scenario);

    // Act
    var result = await ModelTestHelpers.RunSingleAttemptAsync(bridge, scenario);

    // Assert
    result.ShouldMatchExpectedOutcome();
}
```

### Statistical Testing
```csharp
[Fact]
public async Task StatisticalTest()
{
    var results = await ModelTestHelpers.RunMultipleAttemptsAsync(
        bridge, scenario, attemptCount: 10);
    
    results.ShouldHaveMinimumSuccessRate(0.8);
    results.ShouldHaveAverageTimeWithinBounds(TimeSpan.FromSeconds(5));
}
```

### Conditional Execution
```csharp
[Fact]
public void AiDependentTest()
{
    TestConfigurationHelper.SkipIfAiModelsNotConfigured("HighConfidence");
    
    // Test logic here...
}
```

## Benefits

1. **Consistency**: Standardized test patterns across all integration tests
2. **Reusability**: Common scenarios and helpers reduce code duplication
3. **Maintainability**: Centralized configuration and assertion logic
4. **Scalability**: Easy to add new scenarios and assertion types
5. **Debugging**: Rich error reporting and statistical analysis
6. **CI/CD Friendly**: Configurable execution based on environment

## Next Steps

This infrastructure supports the implementation of:
- **Task 015.3**: High-Confidence Extraction Tests
- **Task 015.4**: Progressive Complexity Tests  
- **Task 015.5**: Resilience and Error Handling Tests
- **Task 015.6**: Multi-Model Comparison Tests

Each subsequent task can leverage these shared components for consistent, maintainable integration testing.