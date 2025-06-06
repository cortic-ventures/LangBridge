using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests.Implementation.TypeSystem;

public class TypeNameMapperTests
{
    #region Basic Type Mapping Tests

    [Theory]
    [InlineData(typeof(int), false, "number")]
    [InlineData(typeof(int), true, "integer")]
    [InlineData(typeof(decimal), false, "number")]
    [InlineData(typeof(decimal), true, "decimal")]
    [InlineData(typeof(bool), false, "boolean")]
    [InlineData(typeof(bool), true, "boolean")]
    [InlineData(typeof(string), false, "string")]
    [InlineData(typeof(string), true, "string")]
    [InlineData(typeof(byte), false, "number")]
    [InlineData(typeof(byte), true, "integer")]
    [InlineData(typeof(short), false, "number")]
    [InlineData(typeof(short), true, "integer")]
    [InlineData(typeof(long), false, "number")]
    [InlineData(typeof(long), true, "integer")]
    [InlineData(typeof(float), false, "number")]
    [InlineData(typeof(float), true, "decimal")]
    [InlineData(typeof(double), false, "number")]
    [InlineData(typeof(double), true, "decimal")]
    public void GetLLMFriendlyTypeName_BasicTypes(Type type, bool includeFormatHints, string expected)
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(type, includeFormatHints);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region DateTime Format Hint Tests

    [Theory]
    [InlineData(typeof(DateTime), false, "string")]
    [InlineData(typeof(DateTime), true, "datetime (assume 00:00:00 if time component missing)")]
    [InlineData(typeof(DateTimeOffset), false, "string")]
    [InlineData(typeof(DateTimeOffset), true, "datetime (assume 00:00:00 if time component missing)")]
    [InlineData(typeof(TimeSpan), false, "string")]
    [InlineData(typeof(TimeSpan), true, "time-iso")]
    [InlineData(typeof(Guid), false, "string")]
    [InlineData(typeof(Guid), true, "uuid")]
    public void GetLLMFriendlyTypeName_WithFormatHints(Type type, bool includeFormatHints, string expected)
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(type, includeFormatHints);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_DateOnly_WithFormatHints()
    {
        // Arrange - Use reflection to get DateOnly if available
        var dateOnlyType = Type.GetType("System.DateOnly");
        
        if (dateOnlyType != null)
        {
            // Act
            var withHints = TypeNameMapper.GetLLMFriendlyTypeName(dateOnlyType, true);
            var withoutHints = TypeNameMapper.GetLLMFriendlyTypeName(dateOnlyType, false);

            // Assert
            Assert.Equal("date-iso", withHints);
            Assert.Equal("string", withoutHints);
        }
        // DateOnly type not available in this runtime - skip test
    }

    [Fact]
    public void GetLLMFriendlyTypeName_TimeOnly_WithFormatHints()
    {
        // Arrange - Use reflection to get TimeOnly if available
        var timeOnlyType = Type.GetType("System.TimeOnly");
        
        if (timeOnlyType != null)
        {
            // Act
            var withHints = TypeNameMapper.GetLLMFriendlyTypeName(timeOnlyType, true);
            var withoutHints = TypeNameMapper.GetLLMFriendlyTypeName(timeOnlyType, false);

            // Assert
            Assert.Equal("time-iso", withHints);
            Assert.Equal("string", withoutHints);
        }
        // TimeOnly type not available in this runtime - skip test
    }

    #endregion

    #region Nullable Type Tests

    [Theory]
    [InlineData(typeof(int?), true, "integer")]
    [InlineData(typeof(int?), false, "number")]
    [InlineData(typeof(DateTime?), true, "datetime (assume 00:00:00 if time component missing)")]
    [InlineData(typeof(DateTime?), false, "string")]
    [InlineData(typeof(decimal?), false, "number")]
    [InlineData(typeof(decimal?), true, "decimal")]
    [InlineData(typeof(bool?), false, "boolean")]
    [InlineData(typeof(bool?), true, "boolean")]
    [InlineData(typeof(Guid?), false, "string")]
    [InlineData(typeof(Guid?), true, "uuid")]
    public void GetLLMFriendlyTypeName_NullableTypes(Type type, bool includeFormatHints, string expected)
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(type, includeFormatHints);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Enum and Complex Type Tests

    [Fact]
    public void GetLLMFriendlyTypeName_EnumType_ReturnsString()
    {
        // Act
        var resultWithHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(ConsoleColor), true);
        var resultWithoutHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(ConsoleColor), false);

        // Assert
        Assert.Equal("string", resultWithHints);
        Assert.Equal("string", resultWithoutHints);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_CustomEnumType_ReturnsString()
    {
        // Act
        var resultWithHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(TestEnum), true);
        var resultWithoutHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(TestEnum), false);

        // Assert
        Assert.Equal("string", resultWithHints);
        Assert.Equal("string", resultWithoutHints);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_ComplexType_ReturnsAny()
    {
        // Act
        var resultWithHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(ComplexTestClass), true);
        var resultWithoutHints = TypeNameMapper.GetLLMFriendlyTypeName(typeof(ComplexTestClass), false);

        // Assert
        Assert.Equal("any", resultWithHints);
        Assert.Equal("any", resultWithoutHints);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_ObjectType_ReturnsAny()
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(typeof(object), true);

        // Assert
        Assert.Equal("any", result);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_CollectionType_ReturnsAny()
    {
        // Act
        var listResult = TypeNameMapper.GetLLMFriendlyTypeName(typeof(List<string>), true);
        var arrayResult = TypeNameMapper.GetLLMFriendlyTypeName(typeof(int[]), false);
        var dictResult = TypeNameMapper.GetLLMFriendlyTypeName(typeof(Dictionary<string, int>), true);

        // Assert
        Assert.Equal("any", listResult);
        Assert.Equal("any", arrayResult);
        Assert.Equal("any", dictResult);
    }

    #endregion

    #region Method Overload Tests

    [Fact]
    public void GetLLMFriendlyTypeName_WithoutFormatHints_DefaultsToFalse()
    {
        // Arrange
        var type = typeof(DateTime);

        // Act
        var resultWithoutParameter = TypeNameMapper.GetLLMFriendlyTypeName(type);
        var resultWithFalse = TypeNameMapper.GetLLMFriendlyTypeName(type, false);

        // Assert
        Assert.Equal(resultWithFalse, resultWithoutParameter);
        Assert.Equal("string", resultWithoutParameter);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_IntegerType_DifferentResultsBasedOnHints()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var withHints = TypeNameMapper.GetLLMFriendlyTypeName(type, true);
        var withoutHints = TypeNameMapper.GetLLMFriendlyTypeName(type, false);

        // Assert
        Assert.NotEqual(withHints, withoutHints);
        Assert.Equal("integer", withHints);
        Assert.Equal("number", withoutHints);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetLLMFriendlyTypeName_UnsignedTypes_ReturnCorrectMapping()
    {
        // Act & Assert
        Assert.Equal("integer", TypeNameMapper.GetLLMFriendlyTypeName(typeof(uint), true));
        Assert.Equal("number", TypeNameMapper.GetLLMFriendlyTypeName(typeof(uint), false));
        
        Assert.Equal("integer", TypeNameMapper.GetLLMFriendlyTypeName(typeof(ulong), true));
        Assert.Equal("number", TypeNameMapper.GetLLMFriendlyTypeName(typeof(ulong), false));
        
        Assert.Equal("integer", TypeNameMapper.GetLLMFriendlyTypeName(typeof(ushort), true));
        Assert.Equal("number", TypeNameMapper.GetLLMFriendlyTypeName(typeof(ushort), false));
    }

    [Fact]
    public void GetLLMFriendlyTypeName_CharType_ReturnsString()
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(typeof(char), true);

        // Assert
        Assert.Equal("string", result);
    }

    [Fact]
    public void GetLLMFriendlyTypeName_IntPtrType_ReturnsString()
    {
        // Act
        var result = TypeNameMapper.GetLLMFriendlyTypeName(typeof(IntPtr), true);

        // Assert - IntPtr is primitive but maps to string as it's not numeric/boolean
        Assert.Equal("string", result);
    }

    #endregion

    #region Test Helper Classes and Enums

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    private class ComplexTestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public List<string> Items { get; set; } = new();
    }

    #endregion
}