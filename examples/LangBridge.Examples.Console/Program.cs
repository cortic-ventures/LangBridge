using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LangBridge.ContextualBridging;
using LangBridge.Extensions;
using LangBridge.Internal.Infrastructure.Processing;
using LangBridge.Internal.Infrastructure.ContextualBridging;
using LangBridge.Internal.Infrastructure.TypeSystem;
using Microsoft.Extensions.Configuration;


public class UserInvoiceDetails
{
    [Description("The name of the user who received the invoice")]
    public string UserName = string.Empty;
    public Address Address = new();
}

public class Address
{
    public string Street = string.Empty;
    public string City = string.Empty;
    public string Country = string.Empty;
}

[Description("Invoice structure")]
public class InvoiceInfo
{
    [Description("Amount to pay | Order value")]
    public decimal Amount = 0;
    
    [Description("Payment Due | whenever the payment is due by")]
    public DateTime PaymentDueDate = DateTime.Today;
    public string OrderId = string.Empty;
    
    [Description("User Details")]
    public List<UserInvoiceDetails> UserInvoiceDetails = [];
}

public class FinancialAnalysis
{
    [Description("Overall business direction: 'Positive', 'Negative', 'Stable', 'Mixed' based on narrative clues")]
    public string OverallTrajectory { get; set; } = string.Empty;
    
    [Description("Financial metrics that can be inferred like 'HeadcountChange: stable', 'CashRunway: extended', 'Margins: improving'")]
    public Dictionary<string, object> InferredMetrics { get; set; } = new();
    
    [Description("Potential corporate activities mentioned or implied like 'IPO preparation', 'Acquisition target', 'Expansion'")]
    public List<string> PotentialCorporateActions { get; set; } = new();
    
    [Description("Competitive strength from 0.0 to 1.0, where 1.0 is market leader based on comparative statements")]
    public double CompetitivePosition { get; set; }
    
    [Description("Business risks mentioned or implied in the narrative")]
    public List<string> RiskFactors { get; set; } = new();
    
    [Description("Business maturity stage: 'Startup', 'Growth', 'Mature', 'Decline' based on context clues")]
    public string GrowthStage { get; set; } = string.Empty;
    
    [Description("Sentiment of different groups like 'Employees: optimistic', 'Leadership: confident'")]
    public Dictionary<string, string> StakeholderSentiment { get; set; } = new();
}

class Program
{
    static void Main(string[] args)
    {
        // Skip LLM setup for this test - we're only testing TypeSystem functionality
        // var builder = Host.CreateApplicationBuilder(args);
        // builder.Configuration.AddJsonFile("appsettings.json");
        // var kernel = builder.Services.AddKernel();
        // kernel.Services.AddLangBridge(builder.Configuration);
        // var host = builder.Build();
    
        // Test the new schema generator
        var schemaGenerator = new ComprehensiveJsonSchemaGenerator();
        var schema = schemaGenerator.GenerateComprehensiveSchema<InvoiceInfo>();
        Console.WriteLine("Generated Schema for InvoiceInfo:");
        Console.WriteLine(schema);
        Console.WriteLine(new string('=', 50));
        
        // Test the new property paths extraction
        // We need to call the method via reflection since it's private
        var method = typeof(TextContextualBridge).GetMethod("GetTypePropertyPaths", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var genericMethod = method?.MakeGenericMethod(typeof(InvoiceInfo));
        var propertyPaths = (List<string>?)genericMethod?.Invoke(null, new object[] { 5, true });
        
        Console.WriteLine("Property Paths for InvoiceInfo:");
        if (propertyPaths != null)
        {
            foreach (var path in propertyPaths)
            {
                Console.WriteLine($"  {path}");
            }
        }
        Console.WriteLine(new string('=', 50));
        
        // Test enhanced property extraction with descriptions
        TestEnhancedPropertyExtraction();
        Console.WriteLine(new string('=', 50));
        
        Console.WriteLine("All property extraction tests completed successfully!");
        Console.WriteLine("(Skipped LLM-dependent parts that would require Ollama/OpenAI connection)");
    }

    static void TestEnhancedPropertyExtraction()
    {
        Console.WriteLine("Testing Enhanced Property Extraction with Descriptions:");
        Console.WriteLine();

        // Test the enhanced property extraction method with descriptions
        var propertyInfos = TypePropertyPathExtractor.ExtractPropertyInfoWithDescriptions<FinancialAnalysis>();

        Console.WriteLine($"Found {propertyInfos.Count} properties with descriptions:");
        Console.WriteLine();

        foreach (var propertyInfo in propertyInfos)
        {
            Console.WriteLine($"Path: {propertyInfo.Path}");
            Console.WriteLine($"Type: {propertyInfo.TypeName}");
            Console.WriteLine($"Description: {(string.IsNullOrEmpty(propertyInfo.Description) ? "[No description]" : propertyInfo.Description)}");
            Console.WriteLine($"Full Description: {propertyInfo.FullDescription}");
            Console.WriteLine();
        }

        // Verify that descriptions are being extracted correctly
        var trajectoryProperty = propertyInfos.FirstOrDefault(p => p.Path == "OverallTrajectory");
        if (trajectoryProperty != null && !string.IsNullOrEmpty(trajectoryProperty.Description))
        {
            Console.WriteLine("✓ PASS: Description extraction is working correctly!");
            Console.WriteLine($"  Example: {trajectoryProperty.Path} has description: '{trajectoryProperty.Description}'");
        }
        else
        {
            Console.WriteLine("✗ FAIL: Description extraction is not working as expected.");
        }

        Console.WriteLine();
        Console.WriteLine("Enhanced Property Extraction Test Completed!");
    }
}