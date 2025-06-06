# Task 014: TypeSystem Testing Strategy

## Overview
Implement comprehensive unit tests for the centralized TypeSystem components to ensure reliability, maintain backward compatibility, and validate enhanced type naming functionality.

## Prerequisites
- Task 012 completed (TypeSystem implementation)
- Task 013 completed (Component refactoring)

## Test Structure

### Test Project Setup
Create test files in the following structure:
```
tests/LangBridge.Tests/Implementation/TypeSystem/
├── TypeClassifierTests.cs
├── TypeNameMapperTests.cs
└── ReflectionHelperTests.cs
```

## 1. TypeClassifier Tests

**File**: `tests/LangBridge.Tests/Implementation/TypeSystem/TypeClassifierTests.cs`

### Test Categories

#### IsSimpleType Tests
```csharp
[Theory]
[InlineData(typeof(int), true)]
[InlineData(typeof(string), true)]
[InlineData(typeof(decimal), true)]
[InlineData(typeof(DateTime), true)]
[InlineData(typeof(DateOnly), true)]
[InlineData(typeof(TimeOnly), true)]
[InlineData(typeof(Guid), true)]
[InlineData(typeof(ConsoleColor), true)] // Enum
[InlineData(typeof(int?), true)] // Nullable
[InlineData(typeof(List<int>), false)]
[InlineData(typeof(object), false)]
[InlineData(typeof(CustomClass), false)]
public void IsSimpleType_ShouldReturnExpectedResult(Type type, bool expected)
```

#### IsCollectionType Tests
```csharp
[Theory]
[InlineData(typeof(string), false)] // String is not a collection
[InlineData(typeof(int[]), true)]
[InlineData(typeof(List<string>), true)]
[InlineData(typeof(IEnumerable<int>), true)]
[InlineData(typeof(Dictionary<string, int>), true)]
[InlineData(typeof(ArrayList), true)]
[InlineData(typeof(int), false)]
public void IsCollectionType_ShouldReturnExpectedResult(Type type, bool expected)
```

#### Numeric Type Classification Tests
```csharp
[Theory]
[InlineData(typeof(byte), true, true, false)]
[InlineData(typeof(int), true, true, false)]
[InlineData(typeof(decimal), true, false, true)]
[InlineData(typeof(double), true, false, true)]
[InlineData(typeof(string), false, false, false)]
[InlineData(typeof(int?), true, true, false)] // Nullable int
public void NumericTypeClassification_ShouldWorkCorrectly(
    Type type, 
    bool isNumeric, 
    bool isInteger, 
    bool isFloatingPoint)
```

#### DateTime Type Tests
```csharp
[Theory]
[InlineData(typeof(DateTime), true)]
[InlineData(typeof(DateTimeOffset), true)]
[InlineData(typeof(DateOnly), true)]
[InlineData(typeof(TimeOnly), true)]
[InlineData(typeof(TimeSpan), true)]
[InlineData(typeof(DateTime?), true)] // Nullable
[InlineData(typeof(string), false)]
public void IsDateTimeType_ShouldReturnExpectedResult(Type type, bool expected)
```

#### GetCollectionElementType Tests
```csharp
[Theory]
[InlineData(typeof(int[]), typeof(int))]
[InlineData(typeof(List<string>), typeof(string))]
[InlineData(typeof(IEnumerable<decimal>), typeof(decimal))]
[InlineData(typeof(Dictionary<string, int>), typeof(KeyValuePair<string, int>))]
[InlineData(typeof(ArrayList), typeof(object))]
public void GetCollectionElementType_ShouldReturnCorrectType(Type collectionType, Type expectedElementType)
```

### Edge Cases to Test
- Nested nullable types
- Custom collections implementing IEnumerable<T>
- Multi-dimensional arrays
- Generic type definitions vs constructed generic types

## 2. TypeNameMapper Tests

**File**: `tests/LangBridge.Tests/Implementation/TypeSystem/TypeNameMapperTests.cs`

### Basic Type Mapping Tests
```csharp
[Theory]
[InlineData(typeof(int), false, "number")]
[InlineData(typeof(int), true, "integer")]
[InlineData(typeof(decimal), false, "number")]
[InlineData(typeof(decimal), true, "decimal")]
[InlineData(typeof(bool), false, "boolean")]
[InlineData(typeof(bool), true, "boolean")]
[InlineData(typeof(string), false, "string")]
[InlineData(typeof(string), true, "string")]
public void GetLLMFriendlyTypeName_BasicTypes(Type type, bool includeFormatHints, string expected)
```

### DateTime Format Hint Tests
```csharp
[Theory]
[InlineData(typeof(DateTime), false, "string")]
[InlineData(typeof(DateTime), true, "datetime-iso")]
[InlineData(typeof(DateOnly), false, "string")]
[InlineData(typeof(DateOnly), true, "date-iso")]
[InlineData(typeof(TimeOnly), false, "string")]
[InlineData(typeof(TimeOnly), true, "time-iso")]
[InlineData(typeof(Guid), false, "string")]
[InlineData(typeof(Guid), true, "uuid")]
public void GetLLMFriendlyTypeName_WithFormatHints(Type type, bool includeFormatHints, string expected)
```

### Nullable Type Tests
```csharp
[Theory]
[InlineData(typeof(int?), true, "integer")]
[InlineData(typeof(DateTime?), true, "datetime-iso")]
[InlineData(typeof(decimal?), false, "number")]
public void GetLLMFriendlyTypeName_NullableTypes(Type type, bool includeFormatHints, string expected)
```

### Enum and Complex Type Tests
```csharp
[Test]
public void GetLLMFriendlyTypeName_EnumType_ReturnsString()
{
    var result = TypeNameMapper.GetLLMFriendlyTypeName(typeof(ConsoleColor), true);
    Assert.AreEqual("string", result);
}

[Test]
public void GetLLMFriendlyTypeName_ComplexType_ReturnsAny()
{
    var result = TypeNameMapper.GetLLMFriendlyTypeName(typeof(CustomClass), true);
    Assert.AreEqual("any", result);
}
```

## 3. ReflectionHelper Tests

**File**: `tests/LangBridge.Tests/Implementation/TypeSystem/ReflectionHelperTests.cs`

### Property Access Tests
```csharp
public class TestClass
{
    public string PublicProperty { get; set; }
    private string PrivateProperty { get; set; }
    public string WriteOnlyProperty { set { } }
    public string this[int index] => "indexed"; // Should be excluded
}

[Test]
public void GetAccessibleProperties_ShouldReturnOnlyPublicReadableProperties()
{
    var properties = ReflectionHelper.GetAccessibleProperties(typeof(TestClass));
    Assert.AreEqual(1, properties.Count());
    Assert.AreEqual("PublicProperty", properties.First().Name);
}
```

### Field Access Tests
```csharp
public class FieldTestClass
{
    public string PublicField;
    private string PrivateField;
    public static string StaticField;
    public readonly string ReadOnlyField;
}

[Test]
public void GetAccessibleFields_ShouldReturnOnlyPublicInstanceFields()
{
    var fields = ReflectionHelper.GetAccessibleFields(typeof(FieldTestClass));
    Assert.AreEqual(2, fields.Count()); // PublicField and ReadOnlyField
    Assert.IsFalse(fields.Any(f => f.IsStatic));
}
```

### Circular Reference Tests
```csharp
public class CircularParent
{
    public CircularChild Child { get; set; }
}

public class CircularChild
{
    public CircularParent Parent { get; set; }
}

[Test]
public void ValidateForCircularReferences_ShouldThrowForCircularReference()
{
    var visitedTypes = new HashSet<Type> { typeof(CircularParent) };
    
    Assert.Throws<InvalidOperationException>(() => 
        ReflectionHelper.ValidateForCircularReferences(typeof(CircularParent), visitedTypes));
}
```

### Combined Member Access Tests
```csharp
[Test]
public void GetAllAccessibleMembers_ShouldReturnPropertiesAndFields()
{
    var members = ReflectionHelper.GetAllAccessibleMembers(typeof(MixedClass));
    
    // Should include both properties and fields
    Assert.IsTrue(members.Any(m => m is PropertyInfo));
    Assert.IsTrue(members.Any(m => m is FieldInfo));
    
    // Should be ordered by name
    var names = members.Select(m => m.Name).ToList();
    Assert.AreEqual(names.OrderBy(n => n).ToList(), names);
}
```

## 4. Integration Tests

Create integration tests that verify the refactored components work correctly with the new TypeSystem:

**File**: `tests/LangBridge.Tests/Integration/TypeSystemIntegrationTests.cs`

```csharp
[Test]
public async Task TextContextualBridge_WithComplexType_UsesEnhancedTypeNames()
{
    // Arrange
    var bridge = // create bridge instance
    var input = "Product costs $29.99 and was created on 2024-01-15";
    
    // Act
    var result = await bridge.ExtractAsync<Product>(input, "Extract product details");
    
    // Assert
    // Verify that enhanced type names are used in property paths
}

[Test]
public void JsonSchemaGenerator_WithNumericTypes_GeneratesCorrectSchema()
{
    // Test that integer vs decimal distinction is preserved
}
```

## 5. Performance Tests

**File**: `tests/LangBridge.Tests/Performance/TypeSystemPerformanceTests.cs`

```csharp
[Test]
public void TypeClassification_Performance_ShouldBeAcceptable()
{
    var stopwatch = Stopwatch.StartNew();
    
    for (int i = 0; i < 10000; i++)
    {
        TypeClassifier.IsSimpleType(typeof(int));
        TypeClassifier.IsCollectionType(typeof(List<string>));
        TypeNameMapper.GetLLMFriendlyTypeName(typeof(DateTime), true);
    }
    
    stopwatch.Stop();
    Assert.Less(stopwatch.ElapsedMilliseconds, 100); // Should complete in under 100ms
}
```

## Test Data Classes

Create these test classes in a shared test helpers file:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Tags { get; set; }
}

public class ComplexNesting
{
    public NestedClass Nested { get; set; }
    public List<NestedClass> NestedList { get; set; }
}

public class NestedClass
{
    public string Value { get; set; }
    public int? NullableInt { get; set; }
}
```

## Coverage Requirements

- Minimum 90% code coverage for all TypeSystem classes
- 100% coverage for all public methods
- Edge cases and error conditions must be tested
- Performance benchmarks to ensure no regression

## Test Execution Strategy

1. Run tests after each component implementation
2. Use test-driven development for bug fixes
3. Add tests for any edge cases discovered during integration
4. Include tests in CI/CD pipeline

## Success Criteria

1. All tests pass consistently
2. Code coverage meets minimum requirements
3. No performance regression compared to original implementation
4. Tests are maintainable and well-documented
5. Edge cases are properly handled