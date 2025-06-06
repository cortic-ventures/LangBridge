# Task 004: Implement Kernel Factory

## Objective
Create the Semantic Kernel factory implementation that creates configured kernel instances based on model configuration, following the VeilQuest pattern.

## Background
The KernelFactory is responsible for creating Semantic Kernel instances with the appropriate AI service (OpenAI, Anthropic, etc.) based on configuration. It should support multiple providers and be easily extensible.

## Files to Create

### 1. `/src/LangBridge/Implementation/KernelFactory.cs`
Create the kernel factory implementation.

```csharp
namespace LangBridge.Implementation;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using LangBridge.Abstractions;
using LangBridge.Configuration;

/// <summary>
/// Factory implementation for creating configured Semantic Kernel instances.
/// </summary>
public class KernelFactory : IKernelFactory
{
    private readonly LangBridgeOptions _options;
    
    public KernelFactory(IOptions<LangBridgeOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }
    
    /// <inheritdoc/>
    public Kernel CreateReasoningKernel()
    {
        var config = _options.GetModelForRole(ModelRole.Reasoning)
            ?? throw new InvalidOperationException("No reasoning model configured");
            
        return CreateKernel(config);
    }
    
    /// <inheritdoc/>
    public Kernel CreateToolingKernel()
    {
        var config = _options.GetModelForRole(ModelRole.Tooling)
            ?? throw new InvalidOperationException("No tooling model configured");
            
        return CreateKernel(config);
    }
    
    private Kernel CreateKernel(ModelConfig config)
    {
        var builder = Kernel.CreateBuilder();
        
        switch (config.Provider)
        {
            case AiProvider.OpenAI:
                ConfigureOpenAI(builder, config);
                break;
                
            case AiProvider.Anthropic:
                ConfigureAnthropic(builder, config);
                break;
                
            case AiProvider.AzureOpenAI:
                ConfigureAzureOpenAI(builder, config);
                break;
                
            default:
                throw new NotSupportedException($"Provider {config.Provider} is not supported");
        }
        
        return builder.Build();
    }
    
    private void ConfigureOpenAI(IKernelBuilder builder, ModelConfig config)
    {
        if (string.IsNullOrEmpty(config.Endpoint))
        {
            builder.AddOpenAIChatCompletion(
                modelId: config.Model,
                apiKey: config.ApiKey,
                orgId: config.OrganizationId);
        }
        else
        {
            // Custom endpoint (e.g., for OpenRouter or other OpenAI-compatible APIs)
            builder.AddOpenAIChatCompletion(
                modelId: config.Model,
                apiKey: config.ApiKey,
                endpoint: new Uri(config.Endpoint),
                orgId: config.OrganizationId);
        }
    }
    
    private void ConfigureAnthropic(IKernelBuilder builder, ModelConfig config)
    {
        // Note: This assumes Semantic Kernel has Anthropic support
        // If not available, this would need a custom implementation
        #pragma warning disable CS0618 // Using experimental API
        builder.AddAnthropicChatCompletion(
            modelId: config.Model,
            apiKey: config.ApiKey);
        #pragma warning restore CS0618
    }
    
    private void ConfigureAzureOpenAI(IKernelBuilder builder, ModelConfig config)
    {
        if (string.IsNullOrEmpty(config.Endpoint))
            throw new InvalidOperationException("Azure OpenAI requires an endpoint");
            
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: config.Model,
            endpoint: config.Endpoint,
            apiKey: config.ApiKey);
    }
}
```

## Dependencies to Add
Add to the project file:
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.x.x" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.x.x" />
<!-- Add Anthropic connector if available -->
```

## Success Criteria
- Factory creates kernels based on configuration
- Supports OpenAI, Anthropic, and Azure OpenAI providers
- Throws clear exceptions for missing configuration
- Kernels are properly configured with API keys and endpoints
- Code follows the singleton pattern (stateless factory)

## Notes
- Check latest Semantic Kernel version for accurate API usage
- Anthropic support might require additional packages or custom implementation
- Consider adding logging for debugging kernel creation
- Factory should remain stateless for thread safety