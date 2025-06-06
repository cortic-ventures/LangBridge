using LangBridge.Internal.Infrastructure.Processing;
using LangBridge.Tests.Integration.Shared;
using CSharpFunctionalExtensions;

namespace LangBridge.Tests.Integration.TextContextualBridge;

/// <summary>
/// Deterministic integration tests for TextContextualBridge.ExtractAsync&lt;T&gt;() with predictable mock responses.
/// These tests verify our code logic works correctly independent of actual LLM behavior.
/// </summary>
public class DeterministicTests : IntegrationTestBase
{
    #region Simple Type Tests

    [Fact]
    public async Task TryFullExtractionAsync_SimpleType_SuccessfulExtraction_ReturnsSuccess()
    {
        // Arrange
        var input = "The temperature is 25 degrees Celsius.";
        var query = "What is the temperature?";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("25");
        MockDataStructuringModel.WithResponse(new ResultWrapper<int> { Result = 25 });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<int>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public async Task TryFullExtractionAsync_SimpleType_FeasibilityCheckFails_ReturnsFailure()
    {
        // Arrange
        var input = "The weather is sunny today.";
        var query = "What is the temperature?";
        
        ConfigureFailedFeasibilityCheck("Temperature not mentioned in text");

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<int>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Temperature not mentioned in text", result.Error);
    }

    [Fact]
    public async Task TryFullExtractionAsync_SimpleType_DataStructuringFails_ReturnsFailure()
    {
        // Arrange
        var input = "The temperature is 25 degrees Celsius.";
        var query = "What is the temperature?";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("25");
        MockDataStructuringModel.WithAllFailures(); // Returns null

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<int>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Failed to structure extracted data", result.Error);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    public async Task TryFullExtractionAsync_BooleanType_VariousValues_ExtractsCorrectly(string extractedValue, bool expectedResult)
    {
        // Arrange
        var input = "The system is currently active.";
        var query = "Is the system active?";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction(extractedValue);
        MockDataStructuringModel.WithResponse(new ResultWrapper<bool> { Result = expectedResult });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<bool>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

    [Fact]
    public async Task TryFullExtractionAsync_StringType_SuccessfulExtraction_ReturnsCorrectValue()
    {
        // Arrange
        var input = "The product name is 'Advanced Widget Pro'.";
        var query = "What is the product name?";
        var expectedName = "Advanced Widget Pro";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction(expectedName);
        MockDataStructuringModel.WithResponse(new ResultWrapper<string> { Result = expectedName });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<string>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedName, result.Value);
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public async Task TryFullExtractionAsync_ComplexType_SuccessfulExtraction_ReturnsCompleteObject()
    {
        // Arrange
        var input = "John Doe, age 30, works as a Software Engineer.";
        var query = "Extract person information.";
        
        var expectedPerson = new PersonModel
        {
            Name = "John Doe",
            Age = 30,
            Occupation = "Software Engineer"
        };

        // Configure feasibility check for all properties
        ConfigureSuccessfulFeasibilityCheck();
        
        // Configure property extraction
        ConfigureSuccessfulComplexTypeExtraction("John Doe", "30", "Software Engineer");
        
        // Configure successful data structuring
        MockDataStructuringModel.WithResponse(new ResultWrapper<PersonModel> { Result = expectedPerson });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<PersonModel>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("John Doe", result.Value.Name);
        Assert.Equal(30, result.Value.Age);
        Assert.Equal("Software Engineer", result.Value.Occupation);
    }

    [Fact]
    public async Task TryFullExtractionAsync_ComplexType_PartialInformation_ReturnsFailure()
    {
        // Arrange
        var input = "John Doe works as a Software Engineer."; // Missing age
        var query = "Extract complete person information.";
        
        // Configure mixed feasibility responses
        MockReasoningModel
            .WithResponseForKey("Name:string", "YES: Name is available")
            .WithResponseForKey("Age:integer", "NO: Age not mentioned")
            .WithResponseForKey("Occupation:string", "YES: Occupation is available");

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<PersonModel>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Age:integer", result.Error);
        Assert.Contains("Age not mentioned", result.Error);
    }

    [Fact]
    public async Task TryFullExtractionAsync_NestedComplexType_SuccessfulExtraction_ReturnsNestedObject()
    {
        // Arrange
        var input = "Product: Advanced Widget, Price: $99.99, Supplier: TechCorp Inc, Contact: tech@corp.com";
        var query = "Extract product with supplier information.";
        
        var expectedProduct = new ProductWithSupplierModel
        {
            Name = "Advanced Widget",
            Price = 99.99m,
            Supplier = new SupplierModel
            {
                CompanyName = "TechCorp Inc",
                ContactEmail = "tech@corp.com"
            }
        };

        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulComplexTypeExtraction("Advanced Widget", "99.99", "TechCorp Inc", "tech@corp.com");
        MockDataStructuringModel.WithResponse(new ResultWrapper<ProductWithSupplierModel> { Result = expectedProduct });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<ProductWithSupplierModel>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Advanced Widget", result.Value.Name);
        Assert.Equal(99.99m, result.Value.Price);
        Assert.NotNull(result.Value.Supplier);
        Assert.Equal("TechCorp Inc", result.Value.Supplier.CompanyName);
        Assert.Equal("tech@corp.com", result.Value.Supplier.ContactEmail);
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData(null, "valid query")]
    [InlineData("", "valid query")]
    [InlineData("   ", "valid query")]
    public async Task TryFullExtractionAsync_InvalidInput_ThrowsArgumentException(string? input, string query)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            TextContextualBridge.TryFullExtractionAsync<string>(input!, query));
    }

    [Theory]
    [InlineData("valid input", null)]
    [InlineData("valid input", "")]
    [InlineData("valid input", "   ")]
    public async Task TryFullExtractionAsync_InvalidQuery_ThrowsArgumentException(string input, string? query)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            TextContextualBridge.TryFullExtractionAsync<string>(input, query!));
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task TryFullExtractionAsync_CancellationTokenCancelled_PropagatesCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        var input = "Some input text";
        var query = "Some query";

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            TextContextualBridge.TryFullExtractionAsync<string>(input, query, cts.Token));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task TryFullExtractionAsync_NullableType_WithValue_ReturnsValue()
    {
        // Arrange
        var input = "The count is 42.";
        var query = "What is the count?";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("42");
        MockDataStructuringModel.WithResponse(new ResultWrapper<int?> { Result = 42 });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<int?>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryFullExtractionAsync_NullableType_WithNull_ReturnsFailure()
    {
        // Arrange
        var input = "No count information available.";
        var query = "What is the count?";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("null");
        MockDataStructuringModel.WithResponse(new ResultWrapper<int?> { Result = null });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<int?>(input, query);

        // Assert - Currently TextContextualBridge treats null Result as failure
        // This is the current implementation behavior
        Assert.True(result.IsFailure);
        Assert.Equal("Failed to structure extracted data", result.Error);
    }

    [Fact]
    public async Task TryFullExtractionAsync_DateTimeType_ValidFormat_ReturnsCorrectDate()
    {
        // Arrange
        var input = "The event is scheduled for 2024-03-15.";
        var query = "When is the event?";
        var expectedDate = new DateTime(2024, 3, 15);
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("2024-03-15");
        MockDataStructuringModel.WithResponse(new ResultWrapper<DateTime> { Result = expectedDate });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<DateTime>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, result.Value);
    }

    [Fact]
    public async Task TryFullExtractionAsync_EmptyResultWrapper_ReturnsFailure()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("some value");
        MockDataStructuringModel.WithResponse(new ResultWrapper<string> { Result = null! });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<string>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Failed to structure extracted data", result.Error);
    }

    [Fact]
    public async Task TryFullExtractionAsync_NullResultWrapper_ReturnsFailure()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("some value");
        MockDataStructuringModel.WithResponse<ResultWrapper<string>>(null);

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<string>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Failed to structure extracted data", result.Error);
    }

    [Fact]
    public async Task TryFullExtractionAsync_LargeComplexObject_HandlesSuccessfully()
    {
        // Arrange
        var input = "Complex data with many properties...";
        var query = "Extract all information.";
        
        var largeObject = new LargeComplexModel
        {
            Id = 1,
            Name = "Test",
            Description = "A large object for testing",
            CreatedDate = DateTime.Now,
            IsActive = true,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["key3"] = true
            }
        };

        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulComplexTypeExtraction("1", "Test", "A large object for testing", DateTime.Now.ToString(), "true");
        MockDataStructuringModel.WithResponse(new ResultWrapper<LargeComplexModel> { Result = largeObject });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<LargeComplexModel>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public async Task TryFullExtractionAsync_MultipleComplexProperties_PartialFailure_ReturnsDetailedError()
    {
        // Arrange
        var input = "John works as an engineer but age is not mentioned";
        var query = "Extract person with all details";

        // Configure mixed responses - some properties available, others not
        MockReasoningModel
            .WithResponseForKey("Name:string", "YES: Name is John")
            .WithResponseForKey("Age:integer", "NO: Age is not mentioned in the text")
            .WithResponseForKey("Occupation:string", "YES: Occupation is engineer")
            .WithResponseForKey("Email:string", "NO: Email not provided");

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<PersonWithEmailModel>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        var errorMessage = result.Error;
        Assert.Contains("Age:integer", errorMessage);
        Assert.Contains("Age is not mentioned in the text", errorMessage);
        Assert.Contains("Email:string", errorMessage);
        Assert.Contains("Email not provided", errorMessage);
    }

    [Fact]
    public async Task TryFullExtractionAsync_ComplexType_AllPropertiesFail_ReturnsCombinedErrors()
    {
        // Arrange
        var input = "Some irrelevant text.";
        var query = "Extract person information.";

        // Configure all properties to fail feasibility check
        MockReasoningModel
            .WithResponseForKey("Name:string", "NO: Name not mentioned")
            .WithResponseForKey("Age:integer", "NO: Age not available")
            .WithResponseForKey("Occupation:string", "NO: Occupation unknown");

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<PersonModel>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        var errorMessage = result.Error;
        Assert.Contains("Name:string", errorMessage);
        Assert.Contains("Age:integer", errorMessage);
        Assert.Contains("Occupation:string", errorMessage);
        Assert.Contains("Name not mentioned", errorMessage);
        Assert.Contains("Age not available", errorMessage);
        Assert.Contains("Occupation unknown", errorMessage);
    }

    [Fact]
    public async Task TryFullExtractionAsync_EmptyMockResponses_UsesFallback()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract information.";
        
        // Don't configure any specific responses, should use fallback
        MockReasoningModel.Reset(); // Uses default fallback "YES: Information available"
        ConfigureSuccessfulSimpleTypeExtraction("fallback value");
        MockDataStructuringModel.WithResponse(new ResultWrapper<string> { Result = "fallback value" });

        // Act
        var result = await TextContextualBridge.TryFullExtractionAsync<string>(input, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("fallback value", result.Value);
    }

    #endregion

    #region Test Models

    public class PersonModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Occupation { get; set; } = string.Empty;
    }

    public class ProductWithSupplierModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public SupplierModel Supplier { get; set; } = new();
    }

    public class SupplierModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
    }

    public class PersonWithEmailModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class LargeComplexModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion
}