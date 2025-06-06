# Task 007: Create Dependency Injection Extensions

## Objective
Create extension methods for IServiceCollection to simplify LangBridge registration in .NET applications, following standard patterns for library configuration.

## Background
Users should be able to add LangBridge to their applications with a simple `services.AddLangBridge()` call. The extension should handle all internal registrations and configuration binding.

## Files to Create

### 1. `/src/LangBridge/Extensions/ServiceCollectionExtensions.cs`
Create the DI registration extensions.

```csharp
namespace LangBridge.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LangBridge.Abstractions;
using LangBridge.Configuration;
using LangBridge.Implementation;

/// <summary>
/// Extension methods for registering LangBridge services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LangBridge services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLangBridge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<LangBridgeOptions>(
            configuration.GetSection(LangBridgeOptions.SectionName));
            
        // Validate configuration at startup
        services.AddSingleton<IValidateOptions<LangBridgeOptions>>(
            new ValidateLangBridgeOptions());
        
        // Register core services
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddScoped<IReasoningModel, SemanticKernelReasoningModel>();
        services.AddScoped<IToolingModel, SemanticKernelToolingModel>();
        services.AddScoped<ITextContextualBridge, TextContextualBridge>();
        
        return services;
    }
    
    /// <summary>
    /// Adds LangBridge services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLangBridge(
        this IServiceCollection services,
        Action<LangBridgeOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        // Validate configuration at startup
        services.AddSingleton<IValidateOptions<LangBridgeOptions>>(
            new ValidateLangBridgeOptions());
        
        // Register core services
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddScoped<IReasoningModel, SemanticKernelReasoningModel>();
        services.AddScoped<IToolingModel, SemanticKernelToolingModel>();
        services.AddScoped<ITextContextualBridge, TextContextualBridge>();
        
        return services;
    }
}

/// <summary>
/// Validates LangBridge configuration options.
/// </summary>
internal class ValidateLangBridgeOptions : IValidateOptions<LangBridgeOptions>
{
    public ValidateOptionsResult Validate(string? name, LangBridgeOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
```

### 2. `/src/LangBridge/LangBridge.csproj`
Update project file with necessary dependencies.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Package metadata -->
    <PackageId>LangBridge</PackageId>
    <Version>0.0.1-alpha</Version>
    <Authors>Your Name</Authors>
    <Description>A C# library that simplifies structured LLM queries from C# code</Description>
    <PackageTags>llm;ai;semantic-kernel;openai;anthropic</PackageTags>
    <RepositoryUrl>https://github.com/yourusername/LangBridge</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core dependencies -->
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />
    
    <!-- Semantic Kernel -->
    <PackageReference Include="Microsoft.SemanticKernel" Version="1.x.x" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.x.x" />
    
    <!-- JSON -->
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### 3. `/examples/LangBridge.Examples.Console/Program.cs`
Create an example console application.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LangBridge.Abstractions;
using LangBridge.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add LangBridge
        services.AddLangBridge(context.Configuration);
    })
    .Build();

// Example usage
var bridge = host.Services.GetRequiredService<ITextContextualBridge>();

var emailText = @"
    Hi John,
    The invoice for your recent order is $1,234.56.
    Payment is due by March 15, 2024.
    Order ID: ORD-2024-001
    Thanks!
";

// Extract specific data
var amount = await bridge.ExtractAsync<decimal?>(
    emailText, 
    "What is the invoice amount?");
    
Console.WriteLine($"Invoice amount: {amount}");

var dueDate = await bridge.ExtractAsync<DateTime?>(
    emailText,
    "When is the payment due?");
    
Console.WriteLine($"Due date: {dueDate}");

// Extract complex object
public record InvoiceInfo(
    decimal Amount,
    DateTime DueDate,
    string OrderId);
    
var invoice = await bridge.ExtractAsync<InvoiceInfo?>(
    emailText,
    "Extract the invoice details");
    
Console.WriteLine($"Full invoice: {invoice}");
```

### 4. `/examples/LangBridge.Examples.Console/appsettings.json`
Create example configuration.

```json
{
  "LangBridge": {
    "Models": [
      {
        "Role": "Reasoning",
        "Tag": "reasoning-primary",
        "Provider": "OpenAI",
        "Model": "gpt-4-turbo-preview",
        "ApiKey": "${OPENAI_API_KEY}"
      },
      {
        "Role": "Tooling",
        "Tag": "tooling-primary", 
        "Provider": "OpenAI",
        "Model": "gpt-3.5-turbo",
        "ApiKey": "${OPENAI_API_KEY}"
      }
    ]
  }
}
```

## Success Criteria
- Extension methods provide clean API for service registration
- Configuration is properly bound and validated
- Services are registered with appropriate lifetimes
- Example project demonstrates basic usage
- Project file includes all necessary dependencies

## Notes
- Consider adding overloads for different configuration scenarios
- IKernelFactory is singleton (stateless), models are scoped
- Configuration validation happens at startup for fail-fast behavior
- Examples should cover common use cases