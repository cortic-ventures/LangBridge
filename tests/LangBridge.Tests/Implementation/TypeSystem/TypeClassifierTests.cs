using System.Collections;
using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests.Implementation.TypeSystem;

public class TypeClassifierTests
{
    #region IsSimpleType Tests

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(char), true)]
    [InlineData(typeof(ConsoleColor), true)] // Enum
    [InlineData(typeof(int?), true)] // Nullable
    [InlineData(typeof(DateTime?), true)] // Nullable DateTime
    [InlineData(typeof(decimal?), true)] // Nullable decimal
    [InlineData(typeof(List<int>), false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(TypeClassifierTests), false)] // Complex type
    [InlineData(typeof(int[]), false)] // Array
    [InlineData(typeof(Dictionary<string, int>), false)]
    public void IsSimpleType_ShouldReturnExpectedResult(Type type, bool expected)
    {
        // Act
        var result = TypeClassifier.IsSimpleType(type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSimpleType_WithDateOnlyType_ReturnsTrue()
    {
        // Arrange - Use reflection to get DateOnly if available
        var dateOnlyType = Type.GetType("System.DateOnly");
        
        // Act & Assert
        if (dateOnlyType != null)
        {
            Assert.True(TypeClassifier.IsSimpleType(dateOnlyType));
        }
        // DateOnly type not available in this runtime - skip test
    }

    [Fact]
    public void IsSimpleType_WithTimeOnlyType_ReturnsTrue()
    {
        // Arrange - Use reflection to get TimeOnly if available
        var timeOnlyType = Type.GetType("System.TimeOnly");
        
        // Act & Assert
        if (timeOnlyType != null)
        {
            Assert.True(TypeClassifier.IsSimpleType(timeOnlyType));
        }
        // TimeOnly type not available in this runtime - skip test
    }

    #endregion

    #region IsCollectionType Tests

    [Theory]
    [InlineData(typeof(string), false)] // String is not a collection
    [InlineData(typeof(int[]), true)]
    [InlineData(typeof(List<string>), true)]
    [InlineData(typeof(IEnumerable<int>), true)]
    [InlineData(typeof(Dictionary<string, int>), true)]
    [InlineData(typeof(ArrayList), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(ICollection<string>), true)]
    [InlineData(typeof(Queue<int>), true)]
    [InlineData(typeof(Stack<string>), true)]
    public void IsCollectionType_ShouldReturnExpectedResult(Type type, bool expected)
    {
        // Act
        var result = TypeClassifier.IsCollectionType(type);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Numeric Type Classification Tests

    [Theory]
    [InlineData(typeof(byte), true, true, false)]
    [InlineData(typeof(sbyte), true, true, false)]
    [InlineData(typeof(short), true, true, false)]
    [InlineData(typeof(ushort), true, true, false)]
    [InlineData(typeof(int), true, true, false)]
    [InlineData(typeof(uint), true, true, false)]
    [InlineData(typeof(long), true, true, false)]
    [InlineData(typeof(ulong), true, true, false)]
    [InlineData(typeof(float), true, false, true)]
    [InlineData(typeof(double), true, false, true)]
    [InlineData(typeof(decimal), true, false, true)]
    [InlineData(typeof(string), false, false, false)]
    [InlineData(typeof(bool), false, false, false)]
    [InlineData(typeof(int?), true, true, false)] // Nullable int
    [InlineData(typeof(decimal?), true, false, true)] // Nullable decimal
    public void NumericTypeClassification_ShouldWorkCorrectly(
        Type type, 
        bool isNumeric, 
        bool isInteger, 
        bool isFloatingPoint)
    {
        // Act
        var actualIsNumeric = TypeClassifier.IsNumericType(type);
        var actualIsInteger = TypeClassifier.IsIntegerType(type);
        var actualIsFloatingPoint = TypeClassifier.IsFloatingPointType(type);

        // Assert
        Assert.Equal(isNumeric, actualIsNumeric);
        Assert.Equal(isInteger, actualIsInteger);
        Assert.Equal(isFloatingPoint, actualIsFloatingPoint);
    }

    #endregion

    #region DateTime Type Tests

    [Theory]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(DateTime?), true)] // Nullable
    [InlineData(typeof(DateTimeOffset?), true)] // Nullable
    [InlineData(typeof(TimeSpan?), true)] // Nullable
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(object), false)]
    public void IsDateTimeType_ShouldReturnExpectedResult(Type type, bool expected)
    {
        // Act
        var result = TypeClassifier.IsDateTimeType(type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsDateTimeType_WithDateOnlyType_ReturnsTrue()
    {
        // Arrange - Use reflection to get DateOnly if available
        var dateOnlyType = Type.GetType("System.DateOnly");
        
        // Act & Assert
        if (dateOnlyType != null)
        {
            Assert.True(TypeClassifier.IsDateTimeType(dateOnlyType));
        }
        // DateOnly type not available in this runtime - skip test
    }

    [Fact]
    public void IsDateTimeType_WithTimeOnlyType_ReturnsTrue()
    {
        // Arrange - Use reflection to get TimeOnly if available
        var timeOnlyType = Type.GetType("System.TimeOnly");
        
        // Act & Assert
        if (timeOnlyType != null)
        {
            Assert.True(TypeClassifier.IsDateTimeType(timeOnlyType));
        }
        // TimeOnly type not available in this runtime - skip test
    }

    #endregion

    #region GetCollectionElementType Tests

    [Theory]
    [InlineData(typeof(int[]), typeof(int))]
    [InlineData(typeof(string[]), typeof(string))]
    [InlineData(typeof(List<string>), typeof(string))]
    [InlineData(typeof(IEnumerable<decimal>), typeof(decimal))]
    [InlineData(typeof(ICollection<bool>), typeof(bool))]
    [InlineData(typeof(Queue<DateTime>), typeof(DateTime))]
    [InlineData(typeof(Stack<object>), typeof(object))]
    public void GetCollectionElementType_ShouldReturnCorrectType(Type collectionType, Type expectedElementType)
    {
        // Act
        var result = TypeClassifier.GetCollectionElementType(collectionType);

        // Assert
        Assert.Equal(expectedElementType, result);
    }

    [Fact]
    public void GetCollectionElementType_WithDictionary_ReturnsKeyValuePair()
    {
        // Arrange
        var dictionaryType = typeof(Dictionary<string, int>);
        var expectedType = typeof(KeyValuePair<string, int>);

        // Act
        var result = TypeClassifier.GetCollectionElementType(dictionaryType);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void GetCollectionElementType_WithArrayList_ReturnsObject()
    {
        // Arrange
        var arrayListType = typeof(ArrayList);

        // Act
        var result = TypeClassifier.GetCollectionElementType(arrayListType);

        // Assert
        Assert.Equal(typeof(object), result);
    }

    [Fact]
    public void GetCollectionElementType_WithMultidimensionalArray_ReturnsElementType()
    {
        // Arrange
        var multiArrayType = typeof(int[,]);

        // Act
        var result = TypeClassifier.GetCollectionElementType(multiArrayType);

        // Assert
        Assert.Equal(typeof(int), result);
    }

    [Fact]
    public void GetCollectionElementType_WithCustomCollection_ReturnsCorrectType()
    {
        // Arrange
        var customCollectionType = typeof(CustomCollection<string>);

        // Act
        var result = TypeClassifier.GetCollectionElementType(customCollectionType);

        // Assert
        Assert.Equal(typeof(string), result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsSimpleType_WithNestedNullable_WorksCorrectly()
    {
        // Note: C# doesn't support nested nullables like int??, but test the logic
        var nullableInt = typeof(int?);
        
        // Act & Assert
        Assert.True(TypeClassifier.IsSimpleType(nullableInt));
        Assert.True(TypeClassifier.IsNumericType(nullableInt));
        Assert.True(TypeClassifier.IsIntegerType(nullableInt));
    }

    [Fact]
    public void GetCollectionElementType_WithGenericTypeDefinition_ReturnsFirstGenericArg()
    {
        // Arrange
        var genericListType = typeof(List<>);

        // Act
        var result = TypeClassifier.GetCollectionElementType(genericListType);

        // Assert - Should return object when can't determine specific type
        Assert.Equal(typeof(object), result);
    }

    #endregion

    #region Test Helper Classes

    private class CustomCollection<T> : IEnumerable<T>
    {
        private readonly List<T> _items = new();

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    #endregion
}