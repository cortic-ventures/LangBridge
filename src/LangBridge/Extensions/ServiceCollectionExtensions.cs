using System.ClientModel;
using LangBridge.ContextualBridging;
using LangBridge.Configuration;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Abstractions.Processing;
using LangBridge.Internal.Infrastructure.ContextualBridging;
using LangBridge.Internal.Infrastructure.LanguageModels;
using LangBridge.Internal.Infrastructure.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;

namespace LangBridge.Extensions;

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

        // Add logging services
        services.AddLogging();
 
        // Register core services
        services.AddScoped<IReasoningModel, ReasoningModel>();
        services.AddScoped<IDataStructuringModel, DataStructuringModel>();
        services.AddScoped<ITextContextualBridge, TextContextualBridge>();
        services.AddScoped<IComprehensiveJsonSchemaGenerator, ComprehensiveJsonSchemaGenerator>();
        RegisterLanguageModels(services, configuration.GetSection(LangBridgeOptions.SectionName));
        return services;
    }

    private static void RegisterLanguageModels(IServiceCollection services, IConfigurationSection configuration)
    {
        var modelConfigs = configuration.GetRequiredSection("Models").Get<List<ModelConfig>>()!;

        modelConfigs.ForEach(modelConfig =>
        {
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _ = modelConfig.Provider switch
            {
                AiProvider.Ollama => AddOllama(services, modelConfig),
                AiProvider.AzureOpenAI => AddOpenAiCompatibleInterface(services, modelConfig),
                AiProvider.OpenAI => AddOpenAiCompatibleInterface(services, modelConfig),
                AiProvider.OpenRouter => AddOpenAiCompatibleInterface(services, modelConfig),
                AiProvider.Groq => AddOpenAiCompatibleInterface(services, modelConfig),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
    }


    private static IServiceCollection AddOllama(IServiceCollection services, ModelConfig modelConfig)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(modelConfig.Endpoint ?? ""),
            Timeout = TimeSpan.FromMinutes(10), // Increased timeout for better reliability
        };

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return services.AddOllamaChatCompletion(modelConfig.ModelName,
            httpClient: httpClient,
            serviceId: modelConfig.Purpose.ToString());
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    private static IServiceCollection AddOpenAiCompatibleInterface(IServiceCollection services, ModelConfig modelConfig)
    {
        var apiKeyCredential = new ApiKeyCredential(modelConfig.ApiKey!);
        var openAiClientOptions = new OpenAIClientOptions
            { Endpoint = new Uri(modelConfig.Endpoint ?? ""), NetworkTimeout = TimeSpan.FromMinutes(3) };
        var openAiClient = new OpenAIClient(apiKeyCredential, openAiClientOptions);

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return services.AddOpenAIChatCompletion(modelConfig.ModelName,
            openAIClient: openAiClient,
            serviceId: modelConfig.Purpose.ToString());
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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