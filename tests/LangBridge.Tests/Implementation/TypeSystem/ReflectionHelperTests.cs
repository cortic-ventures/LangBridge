using System.Reflection;
using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests.Implementation.TypeSystem;

public class ReflectionHelperTests
{
    [Fact]
    public void GetAccessibleProperties_ShouldReturnOnlyPublicReadableProperties()
    {
        // Act
        var properties = ReflectionHelper.GetAccessibleProperties(typeof(TestClass)).ToList();

        // Assert
        Assert.Equal(2, properties.Count);
        Assert.Contains(properties, p => p.Name == "PublicProperty");
        Assert.Contains(properties, p => p.Name == "ReadOnlyProperty");
        Assert.DoesNotContain(properties, p => p.Name == "PrivateProperty");
        Assert.DoesNotContain(properties, p => p.Name == "WriteOnlyProperty");
        Assert.DoesNotContain(properties, p => p.Name == "Item"); // Indexed property should be excluded
    }

    [Fact]
    public void GetAccessibleFields_ShouldReturnOnlyPublicInstanceFields()
    {
        // Act
        var fields = ReflectionHelper.GetAccessibleFields(typeof(FieldTestClass)).ToList();

        // Assert
        Assert.Equal(2, fields.Count);
        Assert.Contains(fields, f => f.Name == "PublicField");
        Assert.Contains(fields, f => f.Name == "ReadOnlyField");
        Assert.DoesNotContain(fields, f => f.Name == "PrivateField");
        Assert.DoesNotContain(fields, f => f.Name == "StaticField");
    }

    [Fact]
    public void ValidateForCircularReferences_WithNewType_DoesNotThrow()
    {
        // Arrange
        var visitedTypes = new HashSet<Type> { typeof(string), typeof(int) };

        // Act & Assert - Should not throw
        ReflectionHelper.ValidateForCircularReferences(typeof(DateTime), visitedTypes);
    }

    [Fact]
    public void ValidateForCircularReferences_WithCircularReference_ThrowsInvalidOperationException()
    {
        // Arrange
        var visitedTypes = new HashSet<Type> { typeof(CircularParent), typeof(string) };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            ReflectionHelper.ValidateForCircularReferences(typeof(CircularParent), visitedTypes));

        Assert.Contains("Circular reference detected", exception.Message);
        Assert.Contains("CircularParent", exception.Message);
        Assert.Contains("JsonIgnore", exception.Message);
    }

    [Fact]
    public void GetAllAccessibleMembers_ShouldReturnPropertiesAndFields()
    {
        // Act
        var members = ReflectionHelper.GetAllAccessibleMembers(typeof(MixedClass)).ToList();

        // Assert
        Assert.Contains(members, m => m is PropertyInfo);
        Assert.Contains(members, m => m is FieldInfo);
        
        // Verify specific members are included
        Assert.Contains(members, m => m.Name == "PublicProperty");
        Assert.Contains(members, m => m.Name == "PublicField");
        
        // Verify ordering by name
        var names = members.Select(m => m.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    [Fact]
    public void GetAllAccessibleMembers_ShouldExcludePrivateMembers()
    {
        // Act
        var members = ReflectionHelper.GetAllAccessibleMembers(typeof(MixedClass)).ToList();

        // Assert
        Assert.DoesNotContain(members, m => m.Name == "PrivateProperty");
        Assert.DoesNotContain(members, m => m.Name == "PrivateField");
        Assert.DoesNotContain(members, m => m.Name == "StaticField");
    }

    #region Test Helper Classes

    public class TestClass
    {
        public string PublicProperty { get; set; } = string.Empty;
        public string ReadOnlyProperty { get; } = "readonly";
        private string PrivateProperty { get; set; } = string.Empty;
        public string WriteOnlyProperty { set { } }
        public string this[int index] => "indexed"; // Indexed property - should be excluded
    }

    public class FieldTestClass
    {
        public string PublicField = string.Empty;
        private string PrivateField = string.Empty;
        public static string StaticField = string.Empty;
        public readonly string ReadOnlyField = "readonly";
    }

    public class CircularParent
    {
        public CircularChild? Child { get; set; }
    }

    public class CircularChild
    {
        public CircularParent? Parent { get; set; }
    }

    public class MixedClass
    {
        public string PublicProperty { get; set; } = string.Empty;
        private string PrivateProperty { get; set; } = string.Empty;
        public string PublicField = string.Empty;
        private string PrivateField = string.Empty;
        public static string StaticField = string.Empty;
    }

    #endregion
}