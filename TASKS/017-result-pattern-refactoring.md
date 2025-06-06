# Task 017: Result<T> Pattern Refactoring

## Overview
Refactor TextContextualBridge to use Result<T> pattern instead of throwing exceptions for insufficient information, providing developers with raw failure explanations for flexible error handling.

## Problem Statement
- Current implementation throws generic exceptions when extraction fails
- No visibility into specific property failures
- Developers can't provide meaningful feedback to users
- All-or-nothing approach with poor error communication

## Solution
Replace exception throwing with Result<T> pattern that includes detailed failure explanations.

## Implementation Plan

### 1. Add CSharpFunctionalExtensions Dependency
```xml
<!-- Add to LangBridge.csproj -->
<PackageReference Include="CSharpFunctionalExtensions" Version="2.40.3" />
```

### 2. Update Interface Signature
```csharp
// In ITextContextualBridge.cs
public interface ITextContextualBridge
{
    Task<Result<T>> TryFullExtractionAsync<T>(
        string input, 
        string query, 
        CancellationToken cancellationToken = default) where T : new();
}
```

### 3. Refactor TextContextualBridge Implementation
Replace current exception throwing logic:

**Current (lines 44-46):**
```csharp
var canFulfillQuery = canFulfillPropertiesOfQueryAssessmentResults.All(x => x.StartsWith("yes", StringComparison.CurrentCultureIgnoreCase));
if (!canFulfillQuery)
    throw new Exception("Not enough info to fulfill the presented query.");
```

**New implementation:**
```csharp
var canFulfillQuery = canFulfillPropertiesOfQueryAssessmentResults.All(x => x.StartsWith("yes", StringComparison.CurrentCultureIgnoreCase));
if (!canFulfillQuery)
{
    var failureExplanations = canFulfillPropertiesOfQueryAssessmentResults
        .Zip(propertyNames, (response, property) => new { Response = response, Property = property })
        .Where(x => x.Response.StartsWith("NO", StringComparison.OrdinalIgnoreCase))
        .Select(x => $"{x.Property}: {x.Response.Substring(3).Trim()}") // Remove "NO - " prefix
        .ToList();
    
    var errorMessage = string.Join("; ", failureExplanations);
    return Result.Failure<T>(errorMessage);
}
```

**Also update successful return (line 57):**
```csharp
// Current:
return response is null? default : response.Result;

// New:
return response?.Result != null 
    ? Result.Success(response.Result) 
    : Result.Failure<T>("Failed to structure extracted data");
```

### 4. Update Method Signature and Return Type
```csharp
public async Task<Result<T>> TryFullExtractionAsync<T>(
    string input, 
    string query, 
    CancellationToken cancellationToken = default) where T : new()
{
    // Implementation stays mostly the same, just return types change
}
```

### 5. Update ServiceCollectionExtensions Registration
Ensure the interface registration matches the new signature.

### 6. Update Usage Examples

**Console Application (Program.cs):**
```csharp
// Before:
try 
{
    var invoice = await bridge.ExtractAsync<Invoice>(text, query);
    Console.WriteLine($"Extracted: {invoice?.InvoiceNumber}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed: {ex.Message}");
}

// After:
var result = await bridge.TryFullExtractionAsync<Invoice>(text, query);
if (result.IsSuccess)
{
    Console.WriteLine($"Extracted: {result.Value.InvoiceNumber}");
}
else
{
    Console.WriteLine($"Failed: {result.Error}");
    
    // Optional: Get user-friendly error message
    var friendlyResult = await bridge.TryFullExtractionAsync<string>(
        result.Error, 
        "Convert this technical error into a user-friendly message");
    
    if (friendlyResult.IsSuccess)
    {
        Console.WriteLine($"User message: {friendlyResult.Value}");
    }
}
```

### 7. Update Tests
Update all existing tests to work with Result<T> pattern:
- Change assertions from exception testing to Result.IsFailure testing
- Update success case assertions to check Result.Value
- Add tests for specific error message content

### 8. Update Documentation
- Update README.md examples
- Update XML documentation
- Update CLAUDE.md with new API patterns

## Files to Modify

1. **Core Implementation:**
   - `src/LangBridge/LangBridge.csproj` - Add CSharpFunctionalExtensions
   - `src/LangBridge/ContextualBridging/ITextContextualBridge.cs` - Update interface
   - `src/LangBridge/Internal/Infrastructure/ContextualBridging/TextContextualBridge.cs` - Main refactoring

2. **Usage Examples:**
   - `examples/LangBridge.Examples.Console/Program.cs` - Update example usage

3. **Tests:**
   - All test files in `tests/LangBridge.Tests/` - Update to use Result<T>

4. **Documentation:**
   - `README.md` - Update API examples
   - `CLAUDE.md` - Update coding conventions

## Benefits

1. **Clear Error Communication**: Developers get specific property failure reasons
2. **Flexible Error Handling**: Raw data for logging, processing, or user-friendly conversion
3. **Performance**: No forced error message processing - developers choose what they need
4. **Recursive Processing**: Can use the same API to convert technical errors to user messages
5. **Consistent API**: No exceptions for business logic failures
6. **Future-Proof**: Clean evolution path to partial extraction

## Breaking Changes

- Method name: `ExtractAsync` → `TryFullExtractionAsync`
- Return type: `Task<T?>` → `Task<Result<T>>`
- Error handling: Exceptions → Result.IsFailure

## Migration Guide

```csharp
// Before (v0.0.1):
try 
{
    var result = await bridge.ExtractAsync<MyType>(text, query);
    if (result != null) { /* success */ }
}
catch (Exception) { /* failure */ }

// After (v0.1.0):
var result = await bridge.TryFullExtractionAsync<MyType>(text, query);
if (result.IsSuccess) 
{
    var data = result.Value; // success
}
else 
{
    var error = result.Error; // failure details
}
```

## Success Criteria

1. All existing functionality preserved with Result<T> pattern
2. Detailed error messages for insufficient information scenarios
3. Zero exceptions thrown for business logic failures
4. All tests passing with new API
5. Console example demonstrates both success and failure handling
6. Performance maintained (no unnecessary LLM calls for error processing)

## Estimated Effort

- Core refactoring: ~100 lines changed
- Test updates: ~200 lines changed  
- Documentation updates: ~50 lines changed
- Total: ~350 lines of changes