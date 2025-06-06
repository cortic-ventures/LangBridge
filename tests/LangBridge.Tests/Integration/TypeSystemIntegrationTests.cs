using LangBridge.Internal.Infrastructure.Processing;
using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests.Integration;

public class TypeSystemIntegrationTests
{
    [Fact]
    public void JsonSchemaGenerator_WithEnhancedTypeNames_GeneratesCorrectSchema()
    {
        // Arrange
        var generator = new ComprehensiveJsonSchemaGenerator();

        // Act
        var schema = generator.GenerateComprehensiveSchema(typeof(ProductTestModel));

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("string", schema); // Should contain type information
        
        // Verify enhanced type names are used
        Assert.True(schema.Contains("integer") || schema.Contains("number")); // Id field
        Assert.Contains("string", schema); // Name field
        Assert.True(schema.Contains("decimal") || schema.Contains("number")); // Price field
    }

    [Fact]
    public void TypeSystemIntegration_ComplexNestedType_WorksCorrectly()
    {
        // Arrange
        var complexType = typeof(ComplexNestedModel);

        // Act
        var isSimple = TypeClassifier.IsSimpleType(complexType);
        var isCollection = TypeClassifier.IsCollectionType(complexType);
        var typeName = TypeNameMapper.GetLLMFriendlyTypeName(complexType, true);
        var properties = ReflectionHelper.GetAccessibleProperties(complexType).ToList();

        // Assert
        Assert.False(isSimple);
        Assert.False(isCollection);
        Assert.Equal("any", typeName);
        Assert.True(properties.Count > 0);
    }

    [Fact]
    public void TypeSystemIntegration_CircularReference_ThrowsAppropriateException()
    {
        // Arrange
        var visitedTypes = new HashSet<Type> { typeof(CircularParentModel) };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ReflectionHelper.ValidateForCircularReferences(typeof(CircularParentModel), visitedTypes));

        Assert.Contains("Circular reference detected", exception.Message);
        Assert.Contains("CircularParentModel", exception.Message);
    }

    [Fact]
    public void TypeSystemIntegration_NullableTypes_HandleCorrectly()
    {
        // Arrange
        var nullableIntType = typeof(int?);
        var nullableDateTimeType = typeof(DateTime?);
        var nullableDecimalType = typeof(decimal?);

        // Act & Assert
        Assert.True(TypeClassifier.IsSimpleType(nullableIntType));
        Assert.True(TypeClassifier.IsNumericType(nullableIntType));
        Assert.True(TypeClassifier.IsIntegerType(nullableIntType));
        Assert.Equal("integer", TypeNameMapper.GetLLMFriendlyTypeName(nullableIntType, true));

        Assert.True(TypeClassifier.IsSimpleType(nullableDateTimeType));
        Assert.True(TypeClassifier.IsDateTimeType(nullableDateTimeType));
        Assert.Equal("datetime (assume 00:00:00 if time component missing)", TypeNameMapper.GetLLMFriendlyTypeName(nullableDateTimeType, true));

        Assert.True(TypeClassifier.IsSimpleType(nullableDecimalType));
        Assert.True(TypeClassifier.IsNumericType(nullableDecimalType));
        Assert.True(TypeClassifier.IsFloatingPointType(nullableDecimalType));
        Assert.Equal("decimal", TypeNameMapper.GetLLMFriendlyTypeName(nullableDecimalType, true));
    }

    #region Test Models

    public class ProductTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class ComplexNestedModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public List<string> Items { get; set; } = new();
        public NestedModel Nested { get; set; } = new();
    }

    public class NestedModel
    {
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CircularParentModel
    {
        public string Name { get; set; } = string.Empty;
        public CircularChildModel? Child { get; set; }
    }

    public class CircularChildModel
    {
        public string Value { get; set; } = string.Empty;
        public CircularParentModel? Parent { get; set; }
    }

    #endregion
}