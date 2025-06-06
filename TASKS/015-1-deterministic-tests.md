# Task 015.1: Deterministic Foundation Tests

## Overview
Create mock-based integration tests for `TextContextualBridge.ExtractAsync<T>()` that verify our code logic works correctly with predictable LLM responses.

## Goal
Test our implementation independently of actual LLM behavior by mocking `IReasoningModel` and `IDataStructuringModel` with known responses.

## Scope
1. **Test Infrastructure Setup**
   - Create mock implementations of `IReasoningModel` and `IDataStructuringModel`
   - Set up test fixtures with dependency injection
   - Create base test class for integration tests

2. **Core Logic Tests**
   - JSON schema generation for various types
   - Deserialization with both System.Text.Json and Newtonsoft.Json
   - Property path extraction with nested objects
   - Circular reference detection
   - Type mapping (simple types, complex objects, collections)
   - Error handling for malformed JSON responses

3. **Edge Case Tests**
   - Null/empty responses from models
   - Invalid JSON from data structuring model
   - Timeout scenarios with cancellation tokens
   - Large object graphs
   - Special types (DateTime, enums, nullable types)

## Files to Create
```
tests/LangBridge.Tests/Integration/
├── TextContextualBridge/
│   └── DeterministicTests.cs
└── Shared/
    ├── MockModels.cs              # Mock implementations
    └── IntegrationTestBase.cs     # Base class with common setup
```

## Success Criteria
- 100% success rate (tests our code, not models)
- All code paths covered with predictable inputs
- Clear separation between code logic and model behavior

## Estimated Effort
~200-300 lines of test code