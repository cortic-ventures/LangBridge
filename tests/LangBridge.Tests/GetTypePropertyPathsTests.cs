using LangBridge.Internal.Infrastructure.TypeSystem;

namespace LangBridge.Tests;

public class GetTypePropertyPathsTests
{
    [Fact]
    public void GetTypePropertyPaths_SimpleType_ReturnsBasicPropertiesWithTypes()
    {
        // Arrange & Act
        var paths = GetTypePropertyPaths<SimpleTestClass>();
        
        // Assert
        Assert.Equal(3, paths.Count);
        Assert.Contains("Id:integer", paths);
        Assert.Contains("IsActive:boolean", paths);
        Assert.Contains("Name:string", paths);
    }
    
    [Fact]
    public void GetTypePropertyPaths_NestedType_ReturnsDeepPathsWithTypes()
    {
        // Arrange & Act
        var paths = GetTypePropertyPaths<NestedTestClass>();
        
        // Assert
        Assert.Contains("Details.Age:integer", paths);
        Assert.Contains("Details.Name:string", paths);
        Assert.Contains("Id:integer", paths);
    }
    
    [Fact]
    public void GetTypePropertyPaths_CollectionOfSimpleTypes_ReturnsArrayFormat()
    {
        // Arrange & Act
        var paths = GetTypePropertyPaths<CollectionTestClass>();
        
        // Assert
        Assert.Contains("Numbers: Array<integer>", paths);
        Assert.Contains("Tags: Array<string>", paths);
    }
    
    [Fact]
    public void GetTypePropertyPaths_CollectionOfComplexTypes_ReturnsArrayWithTypedSchema()
    {
        // Arrange & Act
        var paths = GetTypePropertyPaths<ComplexCollectionTestClass>();
        
        // Assert
        var itemsPath = paths.FirstOrDefault(p => p.StartsWith("Items: Array<"));
        Assert.NotNull(itemsPath);
        Assert.Contains("Age:integer", itemsPath);
        Assert.Contains("Name:string", itemsPath);
    }
    
    [Fact]
    public void GetTypePropertyPaths_CircularReference_ThrowsException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            GetTypePropertyPaths<CircularTestClass>());
        
        Assert.Contains("circular reference", exception.Message.ToLower());
        Assert.Contains("CircularTestClass", exception.Message);
    }
    
    [Fact]
    public void GetTypePropertyPaths_InvoiceInfoExample_ReturnsExpectedTypedPaths()
    {
        // Arrange & Act
        var paths = GetTypePropertyPaths<InvoiceInfo>();
        
        // Assert
        Assert.Contains("Amount:decimal", paths);
        Assert.Contains("OrderId:string", paths);
        Assert.Contains("PaymentDueDate:datetime (ISO 8601 format: 'YYYY-MM-DDTHH:mm:ss' with valid dates only, use null if uncertain)", paths);
        Assert.Contains("UserInvoiceDetails: Array<{Name:string}>", paths);
    }
    
    // Helper method to call TypePropertyPathExtractor
    private static List<string> GetTypePropertyPaths<T>(int maxDepth = 5, bool includeTypes = true)
    {
        return TypePropertyPathExtractor.ExtractPropertyPaths<T>(maxDepth, includeTypes);
    }
}

// Test classes
public class SimpleTestClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class NestedTestClass
{
    public int Id { get; set; }
    public PersonDetails Details { get; set; } = new();
}

public class PersonDetails
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class CollectionTestClass
{
    public List<string> Tags { get; set; } = new();
    public int[] Numbers { get; set; } = Array.Empty<int>();
}

public class ComplexCollectionTestClass
{
    public List<PersonDetails> Items { get; set; } = new();
}

public class CircularTestClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CircularTestClass? Parent { get; set; }
    public List<CircularTestClass> Children { get; set; } = new();
}

// Example classes from the main project
public class InvoiceInfo
{
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public DateTime PaymentDueDate { get; set; }
    public List<UserInvoiceDetails> UserInvoiceDetails { get; set; } = new();
}

public class UserInvoiceDetails
{
    public string Name { get; set; } = string.Empty;
}