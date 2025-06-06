# Task 013: Refactor Components to Use Centralized TypeSystem

## Overview
Refactor `TextContextualBridge` and `ComprehensiveJsonSchemaGenerator` to use the centralized TypeSystem components created in Task 012. This eliminates code duplication while maintaining all existing functionality.

## Prerequisites
- Task 012 must be completed (TypeSystem implementation)
- All TypeSystem unit tests must pass

## Requirements

### 1. Refactor TextContextualBridge

**File**: `src/LangBridge/Implementation/TextContextualBridge.cs`

#### Changes Required:

1. **Add using statement**:
   ```csharp
   using LangBridge.Implementation.TypeSystem;
   ```

2. **Remove duplicate methods**:
   - Remove `IsSimpleType()` method (lines 193-212)
   - Remove `IsCollectionType()` method (lines 217-223)
   - Remove `GetCollectionElementType()` method (lines 228-251)
   - Remove `GetSimpleTypeName()` method (lines 256-274)

3. **Update method calls**:
   - Replace `IsSimpleType(type)` with `TypeClassifier.IsSimpleType(type)`
   - Replace `IsCollectionType(type)` with `TypeClassifier.IsCollectionType(type)`
   - Replace `GetCollectionElementType(type)` with `TypeClassifier.GetCollectionElementType(type)`
   - Replace `GetSimpleTypeName(type)` with `TypeNameMapper.GetLLMFriendlyTypeName(type, true)`

4. **Update ProcessMember method** (around line 134):
   ```csharp
   // Change line 148 from:
   var pathWithType = includeTypes ? $"{fullPath}:{GetSimpleTypeName(memberType)}" : fullPath;
   // To:
   var pathWithType = includeTypes ? $"{fullPath}:{TypeNameMapper.GetLLMFriendlyTypeName(memberType, true)}" : fullPath;
   ```

5. **Update collection handling** (around line 158):
   ```csharp
   // Change from:
   var typeName = GetSimpleTypeName(elementType);
   // To:
   var typeName = TypeNameMapper.GetLLMFriendlyTypeName(elementType, true);
   ```

6. **Consider extracting reflection logic**:
   - Evaluate if property/field enumeration (lines 109-123) should use `ReflectionHelper.GetAllAccessibleMembers()`
   - Update circular reference detection to use `ReflectionHelper.ValidateForCircularReferences()`

### 2. Refactor ComprehensiveJsonSchemaGenerator

**File**: `src/LangBridge/Implementation/ComprehensiveJsonSchemaGenerator.cs`

#### Changes Required:

1. **Add using statement**:
   ```csharp
   using LangBridge.Implementation.TypeSystem;
   ```

2. **Remove duplicate methods**:
   - Remove `IsSimpleType()` method (lines 150-165)
   - Remove `GetSimpleTypeName()` method (lines 167-190)
   - Remove `GetCollectionElementType()` method (lines 192-203)

3. **Update method calls**:
   - Replace all `IsSimpleType(type)` with `TypeClassifier.IsSimpleType(type)`
   - Replace all `GetSimpleTypeName(type)` with `TypeNameMapper.GetLLMFriendlyTypeName(type, false)`
   - Replace `GetCollectionElementType(type)` with `TypeClassifier.GetCollectionElementType(type)`

4. **Update GenerateTypeSchema method** (line 21):
   ```csharp
   // Line 27: Update nullable type handling
   var innerSchema = TypeNameMapper.GetLLMFriendlyTypeName(underlyingType, false);
   
   // Line 36-38: Update simple type check
   if (TypeClassifier.IsSimpleType(elementType))
   {
       return $"Array<{TypeNameMapper.GetLLMFriendlyTypeName(elementType, false)}>";
   }
   
   // Line 47-49: Update simple type handling
   if (TypeClassifier.IsSimpleType(type))
   {
       return TypeNameMapper.GetLLMFriendlyTypeName(type, false);
   }
   ```

5. **Update GetRelevantMembers method** (line 114):
   - Consider replacing with `ReflectionHelper.GetAllAccessibleMembers(type)`
   - Ensure ordering by name is preserved

6. **Collection type detection** (line 32):
   ```csharp
   // Change from:
   if (type.IsArray || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type)))
   // To:
   if (TypeClassifier.IsCollectionType(type))
   ```

### 3. Configuration Decision Point

Since `ComprehensiveJsonSchemaGenerator` generates schemas for LLM consumption, decide whether to:
- Use format hints (`includeFormatHints: true`) for better LLM understanding
- Keep generic type names (`includeFormatHints: false`) for backward compatibility
- Make it configurable via constructor parameter or method parameter

**Recommendation**: Use `includeFormatHints: false` by default to maintain backward compatibility, but consider adding an optional parameter in a future enhancement.

## Testing Strategy

### 1. Regression Tests
- Ensure all existing unit tests continue to pass
- Verify that generated property paths remain identical (or intentionally improved)
- Confirm JSON schema generation produces equivalent output

### 2. Integration Tests
- Test with complex nested objects containing circular references
- Verify collection handling works correctly
- Ensure nullable type handling is preserved

### 3. Enhanced Type Tests
If format hints are enabled, create tests to verify:
- Integer types map to "integer" instead of "number"
- Decimal types map to "decimal" instead of "number"
- DateTime types include ISO format hints when appropriate

## Migration Checklist

- [ ] Complete Task 012 (TypeSystem implementation)
- [ ] Create feature branch for refactoring
- [ ] Refactor TextContextualBridge
- [ ] Refactor ComprehensiveJsonSchemaGenerator
- [ ] Run all existing unit tests
- [ ] Add integration tests for refactored code
- [ ] Update XML documentation if needed
- [ ] Code review focusing on:
  - No functionality regression
  - Proper use of centralized types
  - Consistent error handling
  - Performance considerations

## Risk Mitigation

1. **Backward Compatibility**: 
   - Keep public API signatures unchanged
   - Ensure generated output format remains consistent
   - Add compatibility tests if output format changes

2. **Performance**:
   - Profile before and after refactoring
   - Ensure no performance degradation from additional method calls
   - Consider caching type information if needed

3. **Error Handling**:
   - Preserve existing exception types and messages
   - Ensure circular reference detection still works correctly
   - Maintain null handling behavior

## Success Criteria

1. All duplicate type-related code removed from both components
2. All existing unit tests pass without modification
3. Code coverage remains at or above current levels
4. No breaking changes to public APIs
5. Improved maintainability with single source of truth for type logic

## Notes

- This refactoring should be done incrementally with frequent test runs
- Consider using IDE refactoring tools for method extraction
- Document any behavioral changes in commit messages
- If any edge cases are discovered during refactoring, add unit tests first