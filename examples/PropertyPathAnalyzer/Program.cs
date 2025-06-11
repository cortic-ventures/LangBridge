using System;
using System.ComponentModel;
using System.Linq;
using LangBridge.Internal.Infrastructure.TypeSystem;

namespace PropertyPathAnalyzer;

// Copy the ProductComparison class from the test file
public class ProductComparison
{
    [Description("Products being compared in the review")]
    public List<Product> Products { get; set; } = new();
    
    [Description("Comparison categories like 'Performance', 'Cost', 'Ease of use' with scores for each product")]
    public Dictionary<string, ComparisonDimension> Dimensions { get; set; } = new();
    
    [Description("Which product is recommended based on the review")]
    public string RecommendedProduct { get; set; } = string.Empty;
    
    [Description("Why this product is recommended, based on the reviewer's reasoning")]
    public string RecommendationRationale { get; set; } = string.Empty;
    
    public class Product
    {
        [Description("Product name as mentioned in the review")]
        public string Name { get; set; } = string.Empty;
        
        [Description("Overall score from 0.0 to 1.0 based on reviewer's sentiment")]
        public double OverallScore { get; set; }
        
        [Description("Scores for specific features mentioned like 'Speed: 0.8', 'Usability: 0.6'")]
        public Dictionary<string, double> FeatureScores { get; set; } = new();
        
        [Description("Positive aspects mentioned in the review")]
        public List<string> Strengths { get; set; } = new();
        
        [Description("Negative aspects or limitations mentioned")]
        public List<string> Weaknesses { get; set; } = new();
    }
    
    public class ComparisonDimension
    {
        [Description("Name of the comparison category like 'Performance', 'Cost', 'User Experience'")]
        public string Name { get; set; } = string.Empty;
        
        [Description("Score for each product in this dimension from 0.0 to 1.0")]
        public Dictionary<string, double> ProductScores { get; set; } = new();
        
        [Description("Which product performs best in this dimension")]
        public string Winner { get; set; } = string.Empty;
        
        [Description("How important this dimension seems to the reviewer from 0.0 to 1.0")]
        public double ImportanceWeight { get; set; }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ProductComparison Property Path Analysis ===\n");

        // Extract property information with descriptions
        var propertyInfos = TypePropertyPathExtractor.ExtractPropertyInfoWithDescriptions<ProductComparison>();

        Console.WriteLine($"Total property paths found: {propertyInfos.Count}\n");

        // Show all property paths
        Console.WriteLine("All Property Paths:");
        Console.WriteLine("==================");
        foreach (var prop in propertyInfos)
        {
            Console.WriteLine($"Path: {prop.Path}");
            Console.WriteLine($"Type: {prop.TypeName}");
            if (!string.IsNullOrEmpty(prop.Description))
                Console.WriteLine($"Description: {prop.Description}");
            Console.WriteLine($"Full: {prop.FullDescription}");
            Console.WriteLine();
        }

        // Focus on Products-related paths
        Console.WriteLine("\n=== PRODUCTS-RELATED PATHS ===");
        var productsPaths = propertyInfos.Where(p => p.Path.Contains("Products", StringComparison.OrdinalIgnoreCase)).ToList();
        
        Console.WriteLine($"Found {productsPaths.Count} Products-related paths:");
        foreach (var prop in productsPaths)
        {
            Console.WriteLine($"• {prop.Path} : {prop.TypeName}");
        }

        // Focus on OverallScore-related paths
        Console.WriteLine("\n=== OVERALLSCORE-RELATED PATHS ===");
        var scorePaths = propertyInfos.Where(p => p.Path.Contains("OverallScore", StringComparison.OrdinalIgnoreCase)).ToList();
        
        Console.WriteLine($"Found {scorePaths.Count} OverallScore-related paths:");
        foreach (var prop in scorePaths)
        {
            Console.WriteLine($"• {prop.Path} : {prop.TypeName}");
            if (!string.IsNullOrEmpty(prop.Description))
                Console.WriteLine($"  Description: {prop.Description}");
        }

        // Look for any paths with [*] notation
        Console.WriteLine("\n=== PATHS WITH [*] NOTATION ===");
        var arrayPaths = propertyInfos.Where(p => p.Path.Contains("[*]")).ToList();
        
        Console.WriteLine($"Found {arrayPaths.Count} paths with [*] notation:");
        foreach (var prop in arrayPaths)
        {
            Console.WriteLine($"• {prop.Path} : {prop.TypeName}");
            if (!string.IsNullOrEmpty(prop.Description))
                Console.WriteLine($"  Description: {prop.Description}");
        }

        // Show the most interesting paths
        Console.WriteLine("\n=== KEY FINDINGS ===");
        var interestingPaths = propertyInfos.Where(p => 
            p.Path.Contains("Products") || 
            p.Path.Contains("OverallScore") ||
            p.Path.Contains("[*]") ||
            p.Path.Contains("Dimensions")).ToList();

        Console.WriteLine($"Key paths for analysis ({interestingPaths.Count} total):");
        foreach (var prop in interestingPaths)
        {
            Console.WriteLine($"• {prop.FullDescription}");
        }
    }
}