using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LangBridge.ContextualBridging;
using LangBridge.Extensions;
using LangBridge.Internal.Infrastructure.Processing;
using LangBridge.Internal.Infrastructure.ContextualBridging;
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

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json");
                // Add LangBridge
        var kernel = builder.Services.AddKernel();

        kernel.Services.AddLangBridge(builder.Configuration);
        var host = builder.Build();
    
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
    
        // Example usage
        var bridge = host.Services.GetRequiredService<ITextContextualBridge>();

           var emailText = @"
               Hi John,
               The invoice for your recent order is $1,234.56.
               Payment is due by March 15, 2024.
               Order ID: ORD-2024-001
               Zurcherstrasse 31, Zurich, Switzerland
               Thanks!
           ";
           
        // Extract complex object using new Result<T> pattern
        var invoiceResult = await bridge.TryFullExtractionAsync<InvoiceInfo>(
            emailText,
            "Extract the invoice details");
            
        if (invoiceResult.IsSuccess)
        {
            var invoice = invoiceResult.Value;
            Console.WriteLine($"Extraction successful!");
            Console.WriteLine($"Amount: ${invoice.Amount}");
            Console.WriteLine($"Due Date: {invoice.PaymentDueDate:yyyy-MM-dd}");
            Console.WriteLine($"Order ID: {invoice.OrderId}");
            if (invoice.UserInvoiceDetails.Any())
            {
                var user = invoice.UserInvoiceDetails.First();
                Console.WriteLine($"User: {user.UserName}");
                Console.WriteLine($"Address: {user.Address.Street}, {user.Address.City}, {user.Address.Country}");
            }
        }
        else
        {
            Console.WriteLine($"Extraction failed: {invoiceResult.Error}");
            Console.WriteLine();
            
            // Note: For user-friendly error messages, you would typically use a separate
            // service or a wrapper class since string doesn't have a parameterless constructor
            Console.WriteLine("Raw technical error for debugging purposes.");
        }
        
        Console.WriteLine("Property paths extraction test completed successfully!");
    }
}