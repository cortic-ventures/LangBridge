# LangBridge Implementation Tasks

## Project Overview
LangBridge is a C# library that provides atomic, reliable operations for extracting structured insights from unstructured text, designed specifically for building robust AI agents and workflows.

## Prerequisites
Before starting any task:
1. Ensure you have .NET SDK 8.0+ installed
2. Read the CLAUDE.md file in the project root for coding conventions
3. Complete tasks in numerical order (001 → 008)

## Task Execution Order
Tasks must be completed in order due to dependencies:

1. **001-project-structure.md** - Set up solution and folder structure (DO THIS FIRST!)
2. **002-core-interfaces.md** - Define core abstractions
3. **003-llm-model-interfaces.md** - Define LLM model interfaces  
4. **004-configuration-models.md** - Create configuration classes
5. **005-kernel-factory-implementation.md** - Implement Semantic Kernel factory
6. **006-llm-model-implementations.md** - Implement model abstractions
7. **007-text-contextual-bridge.md** - Create main bridge (scaffold only)
8. **008-dependency-injection.md** - Set up DI and create examples

## Shared Conventions

### Namespaces
- Root: `LangBridge`
- Abstractions: `LangBridge.Abstractions`
- Implementations: `LangBridge.Implementation`
- Configuration: `LangBridge.Configuration`
- Extensions: `LangBridge.Extensions`

### Dependencies
All implementations use these packages (versions specified in task 008):
- Microsoft.SemanticKernel
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- System.Text.Json

### Design Principles
1. **Minimal API Surface** - Keep interfaces simple and focused
2. **Null as "Unknown"** - Return `null` when information cannot be extracted
3. **Stateless Services** - All services should be thread-safe
4. **SK Abstraction** - Semantic Kernel usage should be hidden from consumers

### Testing Your Implementation
After completing each task:
1. Run `dotnet build` from the solution root
2. Ensure no compilation errors
3. Check that XML documentation is complete

## Notes for Implementers
- The core `ExtractAsync` logic in task 007 is intentionally left as a scaffold
- Focus on getting the structure right; optimization comes later
- If you encounter issues with package versions, check the latest Semantic Kernel documentation
- API keys should never be hardcoded - use configuration or environment variables

## Questions?
If a task seems unclear:
1. Check if other tasks provide context
2. Review the interfaces defined in tasks 002-003
3. Look at the example in task 008 for usage patterns