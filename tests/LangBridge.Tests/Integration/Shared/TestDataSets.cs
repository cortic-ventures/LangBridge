using System.ComponentModel.DataAnnotations;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Common test data sets for integration testing scenarios.
/// </summary>
public static class TestDataSets
{
    #region Simple Type Scenarios

    public static class SimpleTypes
    {
        public static readonly TestScenario<string> PersonName = new(
            Input: "John Smith is a software engineer who lives in Seattle.",
            Query: "What is the person's name?",
            ExpectedOutput: "John Smith"
        );

        public static readonly TestScenario<int> PersonAge = new(
            Input: "Sarah is 25 years old and works as a teacher.",
            Query: "How old is Sarah?",
            ExpectedOutput: 25
        );

        public static readonly TestScenario<bool> IsEmployed = new(
            Input: "Mike currently works at Google as a software developer.",
            Query: "Is Mike employed?",
            ExpectedOutput: true
        );

        public static readonly TestScenario<decimal> Price = new(
            Input: "The laptop costs $1,299.99 and comes with a warranty.",
            Query: "What is the price of the laptop?",
            ExpectedOutput: 1299.99m
        );

        public static readonly TestScenario<DateTime> EventDate = new(
            Input: "The conference is scheduled for March 15, 2024.",
            Query: "When is the conference?",
            ExpectedOutput: new DateTime(2024, 3, 15)
        );
    }

    #endregion

    #region Complex Type Scenarios

    public static class ComplexTypes
    {
        public static readonly TestScenario<Person> PersonExtraction = new(
            Input: "Dr. Emily Johnson, age 42, works as a cardiologist at Seattle Medical Center. She can be reached at emily.johnson@smc.com.",
            Query: "Extract the person's information",
            ExpectedOutput: new Person
            {
                Name = "Dr. Emily Johnson",
                Age = 42,
                Email = "emily.johnson@smc.com"
            }
        );

        public static readonly TestScenario<Product> ProductExtraction = new(
            Input: "The iPhone 15 Pro costs $1,199 and has 256GB storage. It features a titanium design and advanced camera system.",
            Query: "Extract the product details",
            ExpectedOutput: new Product
            {
                Name = "iPhone 15 Pro",
                Price = 1199m,
                ProductSummary = "titanium design and advanced camera system"
            }
        );

        public static readonly TestScenario<Address> AddressExtraction = new(
            Input: "The office is located at 123 Main Street, Suite 500, in downtown Seattle, Washington 98101.",
            Query: "Extract the address information",
            ExpectedOutput: new Address
            {
                Street = "123 Main Street, Suite 500",
                City = "Seattle",
                State = "Washington",
                ZipCode = "98101"
            }
        );
    }

    #endregion

    #region Edge Case Scenarios

    public static class EdgeCases
    {
        public static readonly TestScenario<string> MissingInformation = new(
            Input: "The event was great and everyone enjoyed it.",
            Query: "What was the speaker's name?",
            ExpectedOutput: null, // Should fail due to missing information
            ShouldSucceed: false
        );

        public static readonly TestScenario<int> AmbiguousInformation = new(
            Input: "There were 50 attendees, 3 speakers, and 10 staff members.",
            Query: "How many people were there?",
            ExpectedOutput: 0, // Should fail due to ambiguity
            ShouldSucceed: false
        );

        public static readonly TestScenario<Person> PartialInformation = new(
            Input: "John works as an engineer but we don't know his age or contact information.",
            Query: "Extract person details",
            ExpectedOutput: null, // Should fail due to missing required fields
            ShouldSucceed: false
        );
    }

    #endregion

    #region Test Data Models

    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            return obj is Person other &&
                   Name == other.Name &&
                   Age == other.Age &&
                   Email == other.Email;
        }

        public override int GetHashCode() => HashCode.Combine(Name, Age, Email);
    }

    public class Product
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ProductSummary { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            return obj is Product other &&
                   Name == other.Name &&
                   Price == other.Price &&
                   ProductSummary == other.ProductSummary;
        }

        public override int GetHashCode() => HashCode.Combine(Name, Price, ProductSummary);
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            return obj is Address other &&
                   Street == other.Street &&
                   City == other.City &&
                   State == other.State &&
                   ZipCode == other.ZipCode;
        }

        public override int GetHashCode() => HashCode.Combine(Street, City, State, ZipCode);
    }

    #endregion
}

/// <summary>
/// Represents a test scenario with input, query, expected output, and success criteria.
/// </summary>
/// <typeparam name="T">The expected return type.</typeparam>
public record TestScenario<T>(
    string Input,
    string Query,
    T? ExpectedOutput,
    bool ShouldSucceed = true,
    string? Description = null)
{
    /// <summary>
    /// Gets a descriptive name for this test scenario.
    /// </summary>
    public string Name => Description ?? $"{typeof(T).Name}_Extraction";
}