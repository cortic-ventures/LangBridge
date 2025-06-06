using System.Diagnostics;
using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests.Performance;

public class TypeSystemPerformanceTests
{
    private const int PerformanceIterations = 10000;
    private const int AcceptableMilliseconds = 100;

    [Fact]
    public void TypeClassification_Performance_ShouldBeAcceptable()
    {
        // Arrange
        var types = new[]
        {
            typeof(int), typeof(string), typeof(decimal), typeof(DateTime),
            typeof(bool), typeof(List<string>), typeof(Dictionary<string, int>),
            typeof(int?), typeof(DateTime?), typeof(object)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < PerformanceIterations; i++)
        {
            foreach (var type in types)
            {
                TypeClassifier.IsSimpleType(type);
                TypeClassifier.IsCollectionType(type);
                TypeClassifier.IsNumericType(type);
                TypeClassifier.IsIntegerType(type);
                TypeClassifier.IsFloatingPointType(type);
                TypeClassifier.IsDateTimeType(type);
            }
        }
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < AcceptableMilliseconds, 
            $"Type classification should complete in under {AcceptableMilliseconds}ms but took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void TypeNameMapping_Performance_ShouldBeAcceptable()
    {
        // Arrange
        var types = new[]
        {
            typeof(int), typeof(string), typeof(decimal), typeof(DateTime),
            typeof(bool), typeof(List<string>), typeof(Dictionary<string, int>),
            typeof(int?), typeof(DateTime?), typeof(object), typeof(Guid),
            typeof(float), typeof(double), typeof(byte), typeof(ConsoleColor)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < PerformanceIterations; i++)
        {
            foreach (var type in types)
            {
                TypeNameMapper.GetLLMFriendlyTypeName(type, false);
                TypeNameMapper.GetLLMFriendlyTypeName(type, true);
            }
        }
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < AcceptableMilliseconds,
            $"Type name mapping should complete in under {AcceptableMilliseconds}ms but took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ReflectionHelper_Performance_ShouldBeAcceptable()
    {
        // Arrange
        var types = new[]
        {
            typeof(PerformanceTestClass), typeof(string), typeof(List<string>),
            typeof(Dictionary<string, int>), typeof(DateTime), typeof(ComplexTestClass)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < PerformanceIterations / 10; i++) // Reduce iterations for reflection operations
        {
            foreach (var type in types)
            {
                ReflectionHelper.GetAccessibleProperties(type).ToList();
                ReflectionHelper.GetAccessibleFields(type).ToList();
                ReflectionHelper.GetAllAccessibleMembers(type).ToList();
            }
        }
        
        stopwatch.Stop();

        // Assert
        var adjustedLimit = AcceptableMilliseconds * 5; // Reflection is naturally slower
        Assert.True(stopwatch.ElapsedMilliseconds < adjustedLimit,
            $"Reflection operations should complete in under {adjustedLimit}ms but took {stopwatch.ElapsedMilliseconds}ms");
    }

    #region Test Helper Classes

    public class PerformanceTestClass
    {
        public string StringProperty { get; set; } = string.Empty;
        public int IntProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public List<string> ListProperty { get; set; } = new();
        
        public string StringField = string.Empty;
        public int IntField;
        public readonly bool ReadOnlyField = true;
        
        private string PrivateProperty { get; set; } = string.Empty;
        private string PrivateField = string.Empty;
    }

    public class ComplexTestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public Guid Identifier { get; set; }
        public ConsoleColor Color { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public NestedTestClass Nested { get; set; } = new();
        
        public string PublicField = string.Empty;
        public int CountField;
        public readonly DateTime CreatedField = DateTime.Now;
        
        private string PrivateProperty { get; set; } = string.Empty;
        #pragma warning disable CS0169
        private int PrivateField;
        #pragma warning restore CS0169
        public static string StaticField = string.Empty;
    }

    public class NestedTestClass
    {
        public string Value { get; set; } = string.Empty;
        public int Number { get; set; }
        public bool Flag { get; set; }
    }

    #endregion
}