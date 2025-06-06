# Task 010: Deep Property Extraction for Nested Objects

## Problem Statement

The current implementation of `GetTypePropertyNames<T>()` in `TextContextualBridge` only returns top-level property names. This causes issues when extracting nested objects because the LLM is asked about parent objects (e.g., "UserInvoiceDetails") rather than specific leaf properties (e.g., "UserInvoiceDetails.Name").

This leads to confused LLM responses where it tries to extract the entire object rather than individual properties, resulting in incorrect structured data extraction.

## Current Behavior

For a class structure like:
```csharp
public class InvoiceInfo
{
    public decimal Amount;
    public string OrderId;
    public DateTime PaymentDueDate;
    public UserInvoiceDetails UserInvoiceDetails;
    public List<InvoiceInfo> Invoices;
}

public class UserInvoiceDetails
{
    public string Name;
}
```

Current output: `["Amount", "OrderId", "PaymentDueDate", "UserInvoiceDetails", "Invoices"]`

## Desired Behavior

The method should return fully qualified property paths:
- Simple properties: `"Amount"`, `"OrderId"`, `"PaymentDueDate"`
- Nested properties: `"UserInvoiceDetails.Name"`
- Collections of simple types: `"Tags: Array<string>"`
- Collections of complex types: `"Invoices: Array<{Amount, OrderId, PaymentDueDate, UserInvoiceDetails.Name}>"`

## Implementation Requirements

1. **Replace `GetTypePropertyNames<T>()` method** in `TextContextualBridge` with a new implementation that:
   - Returns property paths using dot notation for nested objects
   - Handles collections with visual representation for LLM understanding
   - Prevents infinite recursion with circular references
   - Supports both properties and public fields
   - Has configurable max depth (default: 5)

2. **Add helper methods**:
   - `CollectPropertyPaths()` - Recursive property traversal
   - `IsSimpleType()` - Identify primitive/simple types
   - `IsCollectionType()` - Identify collections
   - `GetCollectionElementType()` - Extract element type from collections
   - `GetSimpleTypeName()` - Map CLR types to JSON-friendly names

3. **Collection handling**:
   - Simple collections: `PropertyName: Array<type>`
   - Complex collections: `PropertyName: Array<{prop1, prop2, ...}>`
   - Dictionaries: Consider as `Array<{key, value}>` or skip

4. **Type classification as "simple"**:
   - Primitives (int, bool, double, etc.)
   - string
   - decimal
   - DateTime, DateTimeOffset, TimeSpan
   - Guid
   - Nullable<T> where T is simple
   - Enums

## Technical Details

### Method Signature
```csharp
private static List<string> GetTypePropertyPaths<T>(int maxDepth = 5)
```

### Circular Reference Prevention
- Use `HashSet<Type>` to track visited types
- Create new HashSet instance when recursing into complex properties
- Skip if type already visited

### Property Path Format
- Use dot notation: `Parent.Child.Property`
- Collections: `PropertyName: Array<{schema}>`
- Keep paths ordered alphabetically for consistency

## Testing Considerations

Test with:
1. Simple flat objects
2. Nested objects (multiple levels)
3. Objects with circular references
4. Collections of primitives
5. Collections of complex types
6. Mixed scenarios

## Example Output

For the `InvoiceInfo` class:
```
[
  "Amount",
  "OrderId",
  "PaymentDueDate",
  "UserInvoiceDetails.Name",
  "Invoices: Array<{Amount, OrderId, PaymentDueDate, UserInvoiceDetails.Name}>"
]
```

## Success Criteria

1. The LLM receives specific property paths instead of ambiguous parent object names
2. Collections are clearly represented with their expected structure
3. No infinite recursion occurs with circular references
4. The implementation maintains backward compatibility with simple types
5. The extracted data correctly maps to the nested structure

## Notes

- The schema representation doesn't need to be parseable - it's purely for LLM comprehension
- Consider performance implications for deeply nested objects
- The approach should align with how `ComprehensiveJsonSchemaGenerator` represents types