# Task 009: Rewrite ComprehensiveJsonSchemaGenerator Using Reflection

## Overview
Rewrite the `ComprehensiveJsonSchemaGenerator` to use .NET Reflection instead of NJsonSchema, enabling:
1. Direct type analysis without requiring object instances
2. Extraction of `DescriptionAttribute` from properties and fields
3. Generation of simplified JSON schemas with inline comments for LLM comprehension

## Current Issues
1. **Dependency on object instances**: Current implementation requires `Activator.CreateInstance<T>()` which may fail for types without parameterless constructors
2. **Missing Description attributes**: The NJsonSchema approach doesn't properly extract C# `DescriptionAttribute` values
3. **Limited control**: Using NJsonSchema adds unnecessary complexity for our simple schema generation needs

## Requirements

### Functional Requirements
1. Generate JSON-like schema from type `T` using only reflection
2. Include Description attribute text as comments above each field/property
3. Support both properties and fields (as shown in `InvoiceInfo` example)
4. Handle nested objects and arrays
5. Support nullable types
6. Work without creating instances of the type

### Output Format
For the `InvoiceInfo` class in the example, generate:
```javascript
{
  // Amount to pay | Order value
  "Amount": number,
  // Payment Due | whenever the payment is due by
  "PaymentDueDate": string,
  "OrderId": string
}
```

## Architecture Design

### 1. Update Interface
Remove NJsonSchema dependency from the interface:
```csharp
public interface IComprehensiveJsonSchemaGenerator
{
    /// <summary>
    /// Generates a comprehensive schema representation from a Type.
    /// </summary>
    /// <typeparam name="T">The type to generate schema for.</typeparam>
    /// <returns>A string with the custom schema representation.</returns>
    string GenerateComprehensiveSchema<T>();
    
    /// <summary>
    /// Generates a comprehensive schema representation from a Type.
    /// </summary>
    /// <param name="type">The type to generate schema for.</param>
    /// <returns>A string with the custom schema representation.</returns>
    string GenerateComprehensiveSchema(Type type);
}
```

### 2. Implementation Structure
```csharp
public class ComprehensiveJsonSchemaGenerator : IComprehensiveJsonSchemaGenerator
{
    private readonly HashSet<Type> _visitedTypes = new();
    
    public string GenerateComprehensiveSchema<T>()
        => GenerateComprehensiveSchema(typeof(T));
    
    public string GenerateComprehensiveSchema(Type type)
    {
        _visitedTypes.Clear();
        return GenerateTypeSchema(type, indent: "");
    }
    
    private string GenerateTypeSchema(Type type, string indent)
    {
        // Main logic here
    }
    
    private string GetSimpleTypeName(Type type)
    {
        // Map CLR types to JSON-friendly names
    }
    
    private string GetDescription(MemberInfo member)
    {
        // Extract DescriptionAttribute value
    }
}
```

### 3. Type Mapping Strategy
Map CLR types to JSON-friendly representations:
- `int`, `long`, `decimal`, `float`, `double` → `number`
- `string` → `string`
- `bool` → `boolean`
- `DateTime`, `DateTimeOffset`, `TimeSpan` → `string`
- `Guid` → `string`
- Arrays/Lists → `Array<T>` or `[{ ... }]` for complex types
- Nullable<T> → `T | null`
- Objects → Inline expansion with properties

### 4. Reflection Logic
1. For each type, check if it's:
   - Primitive/simple type → return simple type name
   - Nullable → handle as `type | null`
   - Array/IEnumerable → handle as array
   - Object → expand properties and fields

2. For object types:
   - Use `GetProperties()` and `GetFields()` with appropriate binding flags
   - Extract `DescriptionAttribute` for each member
   - Handle circular references by tracking visited types
   - Recursively process nested types

### 5. Description Attribute Extraction
```csharp
private string GetDescription(MemberInfo member)
{
    var descAttr = member.GetCustomAttribute<DescriptionAttribute>();
    return descAttr?.Description ?? string.Empty;
}
```

## Implementation Steps

1. **Remove NJsonSchema references**
   - Update `IComprehensiveJsonSchemaGenerator` interface
   - Remove NJsonSchema package reference from `LangBridge.csproj`
   - Update any consumers of this interface

2. **Implement reflection-based generator**
   - Create type detection logic
   - Implement recursive type processing
   - Add Description attribute extraction
   - Handle edge cases (circular refs, null, arrays)

3. **Test with examples**
   - Test with `InvoiceInfo` class
   - Test with nested objects
   - Test with arrays and nullable types
   - Test with types lacking parameterless constructors

## Edge Cases to Handle
1. Circular references (e.g., `class Node { Node Next; }`)
2. Generic types
3. Inheritance hierarchies
4. Types without parameterless constructors
5. Private/internal members (decide on visibility rules)
6. Static members (should be excluded)
7. Indexers and computed properties

## Success Criteria
1. Generator works with only Type information (no instances needed)
2. Description attributes appear as comments in output
3. Output format is clean and LLM-friendly
4. All tests pass
5. Performance is acceptable for typical DTO classes

## Testing Approach
Create unit tests for:
1. Simple types (primitives, strings, dates)
2. Objects with Description attributes
3. Nested objects
4. Arrays and collections
5. Nullable types
6. Edge cases listed above

## References
- Current implementation: `/src/LangBridge/Implementation/ComprehensiveJsonSchemaGenerator.cs`
- Example usage: `/examples/LangBridge.Examples.Console/Program.cs`
- Interface: `/src/LangBridge/Implementation/IComprehensiveJsonSchemaGenerator.cs`