# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LangBridge is a C# library that provides atomic, reliable operations for extracting structured insights from unstructured text, designed specifically for building robust AI agents and workflows.

The project is in pre-alpha stage (v0.0.1) with core features:
- `ExtractAsync<T>()` - Extract structured data of any type from text with a single, unified API
- Atomic operations that return `null` when information cannot be determined reliably
- Support for both simple types (bool, int, string) and complex objects
- Two-model architecture: reasoning models for complex analysis, tooling models for structured output

## Implementation Status

**Completed Core Tasks:**
- ✅ Task 001: Project structure and solution setup
- ✅ Task 002: Core interfaces (`ITextContextualBridge`)
- ✅ Task 003: LLM model interfaces (`IReasoningModel`, `IDataStructuringModel`)
- ✅ Task 004: Configuration models (`ModelConfig`, `LangBridgeOptions`)
- ✅ Task 005: Kernel factory implementation (removed - simplified architecture)
- ✅ Task 006: LLM model implementations (`ReasoningModel`, `DataStructuringModel`)
- ✅ Task 007: Main bridge implementation (`TextContextualBridge`, `TypeExtensions`)
- ✅ Task 008: Dependency injection setup (`ServiceCollectionExtensions`, example console application)

**Enhanced Features:**
- ✅ Task 010: Deep property extraction with typed paths for nested objects
- ✅ Comprehensive JSON schema generation for LLM guidance
- ✅ Robust JSON deserialization with mixed serialization libraries
- ✅ Circular reference detection and exception handling
- ✅ Simple type support via `ResultWrapper<T>` for consistent JSON output
- ✅ Enhanced datetime handling with flexible time component guidance

**Type System Refactoring:**
- ✅ Task 011: Architect note for reflection logic refactoring and type system improvements  
- ✅ Task 012: Centralized TypeSystem implementation (`TypeClassifier`, `TypeNameMapper`, `ReflectionHelper`)
- ✅ Task 013: Component refactoring to use enhanced TypeSystem with improved type mapping
- ✅ Task 014: Comprehensive testing strategy with unit and performance tests

**Access Control Implementation:**
- ✅ Selective `internal` access modifier strategy implemented
- ✅ Implementation details hidden from library consumers
- ✅ Strategic `InternalsVisibleTo` for comprehensive testing
- ✅ Clean public API surface with proper encapsulation

**Result Pattern Enhancement:**
- ✅ Task 017: Result<T> pattern refactoring with detailed error reporting
- ✅ Atomic all-or-nothing extraction approach with `CSharpFunctionalExtensions`
- ✅ Enhanced error reporting for property-level extraction failures
- ✅ Backward-compatible API transition from exception-throwing to Result<T> returns

**Architecture Improvements:**
- ✅ TypeSystem refactoring: Moved property path extraction logic to dedicated `TypePropertyPathExtractor`
- ✅ Improved separation of concerns in `TextContextualBridge` (56% code reduction)
- ✅ Consistent TypeSystem patterns with static helper classes

**All core functionality completed!** 🎉

## Integration Testing Strategy

**Comprehensive AI-Dependent Testing Implementation:**
- ✅ **Task 015.1**: Deterministic Foundation Tests - Mock-based tests with 100% predictable outcomes (27 tests)
  - Mock implementations of `IReasoningModel` and `IDataStructuringModel`
  - Deterministic test infrastructure with dependency injection
  - Complete coverage of `TextContextualBridge` logic independent of LLM behavior
- ✅ **Task 015.2**: Test Infrastructure and Helpers - Shared test data and reusable components  
- ✅ **Task 015.3**: High-Confidence Extraction Tests - Simple scenarios with 95%+ expected success (11 tests)
  - Integration tests with real LLM models (OpenAI, Ollama) for high-confidence scenarios
  - Simple and complex type extractions with 95%+ expected success rates
  - Multi-attempt reliability testing with statistical validation
  - Performance benchmarking for simple extraction scenarios
  - Comprehensive timeout handling and diagnostic testing for network issues
- 🔄 **Task 015.4**: Progressive Complexity Tests - Graduated difficulty levels
- 🔄 **Task 015.5**: Resilience and Error Handling Tests - Edge cases and failure modes
- 📋 **Task 015.6**: Multi-Model Comparison Tests - Cross-model performance analysis (optional)

**Current Test Status:** 180 passing tests (including 27 deterministic + 11 high-confidence integration tests)

## Future Roadmap

**Planned Enhancements:**
- 📋 Task 018: Debugging Observer Pattern - Optional observer pattern for debugging that allows developers to register callbacks to capture prompts, responses, and extraction steps without polluting the main API

## Architecture Overview

LangBridge uses a clean layered architecture with clear separation between public API and internal implementation, enforced through strategic access control:

### Public API (ContextualBridging/)
- `ITextContextualBridge` - Main user-facing interface for text processing with deep property extraction
- `IContextualBridge<T>` - Base interface for contextual data extraction from various input types

### Internal Abstractions (Internal/Abstractions/)
- **LanguageModels:** `IReasoningModel`, `IDataStructuringModel` - LLM service abstractions
- **Processing:** `IComprehensiveJsonSchemaGenerator` - Data processing abstractions

### Internal Infrastructure (Internal/Infrastructure/)
- **ContextualBridging:** Core extraction logic implementation
- **LanguageModels:** Semantic Kernel-based LLM implementations  
- **Processing:** JSON schema generation and serialization
- **TypeSystem:** Centralized type analysis and mapping utilities

### Configuration
- `ModelConfig` - Configuration for individual LLM models
- `LangBridgeOptions` - Root configuration class with validation
- `ModelRole` - Enum defining Reasoning vs Tooling model roles
- `AiProvider` - Enum for supported providers (OpenAI, Anthropic, Azure OpenAI)

### Implementation
- Built on Microsoft Semantic Kernel for LLM abstraction
- Multi-provider support (OpenAI, Ollama, Groq, OpenRouter)  
- Deep property extraction with typed paths (e.g., `Name:string`, `Amount:number`)
- Comprehensive JSON schema generation for nested objects and collections
- Mixed JSON serialization approach (System.Text.Json + Newtonsoft.Json for robustness)
- Circular reference detection with clear exception messaging
- Configuration-driven model selection via appsettings.json or environment variables
- Result<T> pattern for atomic operations with detailed error reporting
- Clean separation of concerns with modular TypeSystem utilities

## Project Structure

```
LangBridge/
├── src/LangBridge/
│   ├── ContextualBridging/     # Main public interfaces
│   │   ├── ITextContextualBridge.cs
│   │   └── IContextualBridge.cs
│   ├── Internal/               # Internal implementation details
│   │   ├── Abstractions/       # Internal abstractions
│   │   │   ├── LanguageModels/
│   │   │   │   ├── IReasoningModel.cs
│   │   │   │   └── IDataStructuringModel.cs
│   │   │   └── Processing/
│   │   │       └── IComprehensiveJsonSchemaGenerator.cs
│   │   └── Infrastructure/     # Technical implementations
│   │       ├── ContextualBridging/
│   │       │   └── TextContextualBridge.cs        # Deep property extraction
│   │       ├── LanguageModels/
│   │       │   ├── ReasoningModel.cs              # LLM reasoning operations
│   │       │   ├── DataStructuringModel.cs        # JSON schema-guided structuring
│   │       │   ├── LanguageModelPurposeType.cs    # Model role definitions
│   │       │   └── ChatMessageContentExtension.cs # Semantic Kernel extensions
│   │       ├── Processing/
│   │       │   ├── ComprehensiveJsonSchemaGenerator.cs  # Schema generation
│   │       │   └── ResultWrapper.cs                    # Simple type wrapper
│   │       └── TypeSystem/                         # Centralized type utilities
│   │           ├── TypeClassifier.cs               # Type classification methods
│   │           ├── TypeNameMapper.cs               # LLM-friendly type naming
│   │           ├── ReflectionHelper.cs             # Shared reflection utilities
│   │           └── TypePropertyPathExtractor.cs    # Property path extraction for complex types
│   ├── Configuration/          # Configuration models
│   │   ├── ModelConfig.cs
│   │   ├── LangBridgeOptions.cs
│   │   └── README.md
│   └── Extensions/             # Helper extensions
│       ├── ServiceCollectionExtensions.cs
│       └── TypeExtensions.cs
├── tests/LangBridge.Tests/     # Unit tests
│   ├── GetTypePropertyPathsTests.cs            # Property path extraction tests
│   ├── Implementation/TypeSystem/              # TypeSystem component tests  
│   ├── Integration/                            # Integration tests
│   ├── Performance/                            # Performance tests
│   └── TextContextualBridgeTests.cs            # Main bridge functionality tests
├── examples/                   # Usage examples
│   └── LangBridge.Examples.Console/
│       ├── Program.cs
│       ├── appsettings.json
│       └── LangBridge.Examples.Console.csproj
└── TASKS/                     # Implementation task definitions
```

## Development Commands

```bash
# Build the project
dotnet build

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"

# Run high-confidence integration tests (requires Ollama/OpenAI)
dotnet test --filter "Category=HighConfidence" --logger "console;verbosity=detailed"

# Run diagnostic tests for troubleshooting
dotnet test --filter "Category=Diagnostic" --logger "console;verbosity=detailed"

# Debug mode: Set debugMode = true in HighConfidenceExtractionTests.cs for easier debugging
# Environment variables for AI tests:
# LANGBRIDGE_TEST_TestSettings__RunAiDependentTests=true
# LANGBRIDGE_TEST_APIKEY_OLLAMA=test
```

## Coding Conventions

- Use C# 12.0 with .NET SDK 8.0
- Nullable reference types are **enabled**
- Make all I/O methods `async` with `CancellationToken` support
- Follow C# naming conventions:
  - PascalCase for types, methods, properties, and constants
  - camelCase with underscore prefix (_camelCase) for private/internal fields
  - Static fields should have s_ prefix (s_staticField)
- Prefer expression-bodied members when appropriate
- Use `required` properties in configuration records
- Return `null` to indicate "insufficient information" rather than throwing exceptions
- Throw exceptions for circular references with clear diagnostic messages
- Use typed property paths for enhanced LLM guidance (e.g., `UserName:string`)
- Keep interfaces minimal - prefer composition over complex inheritance
- Abstract away Semantic Kernel details from public APIs
- Use reflection carefully with proper error handling and type validation
- **Access Control Strategy:**
  - Mark implementation classes as `internal` to hide implementation details
  - Keep types used in reflection/DI as `public` (e.g., `ResultWrapper<T>`, `LanguageModelPurposeType`)
  - Use `InternalsVisibleTo` for test assemblies to maintain comprehensive testing
  - Ensure clean public API surface - consumers should only need `ITextContextualBridge` and `ServiceCollectionExtensions`

## Key Features

### Deep Property Extraction
- Supports nested object traversal with dot notation (e.g., `Address.Street:string`)
- Includes type information in property paths for better LLM understanding
- Handles collections with schema preservation (`Items: Array<{Name:string, Age:number}>`)
- Circular reference detection prevents infinite loops with clear error messages

### Enhanced Type System
- JSON-friendly type mapping (`number`, `string`, `boolean`) with format hints (`decimal`, `integer`)
- Smart datetime handling with guidance for missing time components (`datetime (assume 00:00:00 if time component missing)`)
- Collection type detection with element type analysis
- Support for nullable types and enums
- Simple type wrapper (`ResultWrapper<T>`) ensures consistent JSON output for primitive types
- Comprehensive reflection utilities for property and field analysis

### Robust JSON Processing
- Mixed serialization approach for maximum compatibility
- Schema-guided LLM output generation
- Fallback mechanisms for parsing errors
- Type-safe deserialization with error handling

## Git Workflow

- Main branch (`main`) is always stable
- Feature branches: `feat/<topic>`
- Hotfix branches: `hotfix/<topic>`
- All changes must be submitted via Pull Requests
- PRs should be kept under 400 Lines of Code (LOC) where possible
- Include relevant unit tests with changes
- Use Conventional Commits 1.0.0 format
- Feature branches must be squashed into a single commit before merging