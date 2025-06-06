# Task 012: Implement Centralized Type System

## Overview
Create a centralized type system to eliminate code duplication between `TextContextualBridge` and `ComprehensiveJsonSchemaGenerator`. This task focuses on extracting shared type classification, naming, and reflection logic into reusable components.

## Background
Currently, both `TextContextualBridge.cs` and `ComprehensiveJsonSchemaGenerator.cs` contain duplicate implementations of:
- Type classification methods (`IsSimpleType`, `IsCollectionType`)
- Type name mapping (`GetSimpleTypeName`)
- Collection element type extraction (`GetCollectionElementType`)
- Reflection utilities for properties and fields

This duplication violates DRY principles and makes maintenance difficult.

## Requirements

### 1. Create TypeSystem Namespace Structure
Create the following directory structure:
```
src/LangBridge/Implementation/TypeSystem/
├── TypeClassifier.cs
├── TypeNameMapper.cs
└── ReflectionHelper.cs
```

### 2. Implement TypeClassifier Class

**File**: `src/LangBridge/Implementation/TypeSystem/TypeClassifier.cs`

Create a static class with the following methods:

```csharp
public static class TypeClassifier
{
    // Core classification methods
    public static bool IsSimpleType(Type type)
    public static bool IsCollectionType(Type type)
    public static bool IsNumericType(Type type)
    public static bool IsIntegerType(Type type)
    public static bool IsFloatingPointType(Type type)
    public static bool IsDateTimeType(Type type)
    public static Type? GetCollectionElementType(Type collectionType)
}
```

**Implementation Details**:
- `IsSimpleType`: Should handle primitives, string, decimal, DateTime types, DateOnly, TimeOnly, TimeSpan, Guid, enums, and nullable versions
- `IsCollectionType`: Should return true for IEnumerable types except string
- `IsNumericType`: Should detect all numeric types (byte through decimal)
- `IsIntegerType`: Should detect only integer types (byte, short, int, long, and unsigned variants)
- `IsFloatingPointType`: Should detect float, double, and decimal
- `IsDateTimeType`: Should detect DateTime, DateTimeOffset, DateOnly, TimeOnly, and TimeSpan
- `GetCollectionElementType`: Should handle arrays, generic collections, and IEnumerable<T> interfaces

### 3. Implement TypeNameMapper Class

**File**: `src/LangBridge/Implementation/TypeSystem/TypeNameMapper.cs`

Create a static class with enhanced type naming for LLMs:

```csharp
public static class TypeNameMapper
{
    public static string GetLLMFriendlyTypeName(Type type)
    public static string GetLLMFriendlyTypeName(Type type, bool includeFormatHints)
}
```

**Enhanced Type Mapping Requirements**:
- Integer types (int, long, short, byte) → `"integer"`
- Floating-point types (decimal, double, float) → `"decimal"`
- Boolean → `"boolean"`
- String → `"string"`
- DateTime, DateTimeOffset → `"datetime-iso"` (when includeFormatHints=true) or `"string"` (when false)
- DateOnly → `"date-iso"` (when includeFormatHints=true) or `"string"` (when false)
- TimeOnly, TimeSpan → `"time-iso"` (when includeFormatHints=true) or `"string"` (when false)
- Guid → `"uuid"` (when includeFormatHints=true) or `"string"` (when false)
- Enums → `"string"`
- Handle nullable types by checking underlying type

### 4. Implement ReflectionHelper Class

**File**: `src/LangBridge/Implementation/TypeSystem/ReflectionHelper.cs`

Create a static class with shared reflection utilities:

```csharp
public static class ReflectionHelper
{
    public static IEnumerable<PropertyInfo> GetAccessibleProperties(Type type)
    public static IEnumerable<FieldInfo> GetAccessibleFields(Type type)
    public static void ValidateForCircularReferences(Type type, HashSet<Type> visitedTypes)
    public static IEnumerable<MemberInfo> GetAllAccessibleMembers(Type type)
}
```

**Implementation Details**:
- `GetAccessibleProperties`: Return public instance properties that can be read and have no index parameters
- `GetAccessibleFields`: Return public instance fields that are not static
- `ValidateForCircularReferences`: Throw InvalidOperationException with clear message if circular reference detected
- `GetAllAccessibleMembers`: Combine properties and fields, ordered by name

## Code Migration Examples

### Example 1: Migrating IsSimpleType
Current implementations in both files are nearly identical. Extract to `TypeClassifier.IsSimpleType()`.

### Example 2: Migrating GetSimpleTypeName
Current implementation returns generic "number" for all numeric types. New implementation should:
- Use `TypeClassifier.IsIntegerType()` to return "integer"
- Use `TypeClassifier.IsFloatingPointType()` to return "decimal"
- Add format hints when requested

### Example 3: Collection Detection
Both files check for IEnumerable but exclude string. This logic moves to `TypeClassifier.IsCollectionType()`.

## Testing Requirements

Create unit tests to verify:
1. All type classifications work correctly
2. Enhanced type naming provides expected output
3. Nullable type handling works properly
4. Collection element type extraction handles all cases
5. Circular reference detection works as expected

## Success Criteria

1. No duplicate type-related code between components
2. All existing functionality preserved
3. Enhanced type naming improves LLM understanding
4. Code is well-documented with XML comments
5. Unit tests provide comprehensive coverage

## Notes

- Maintain backward compatibility - existing public APIs must not break
- Use consistent null handling patterns throughout
- Consider performance implications of reflection operations
- Ensure thread safety for all static methods