using LangBridge.ContextualBridging;
using LangBridge.Internal.Abstractions.LanguageModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Base class for integration tests with dependency injection setup.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected MockReasoningModel MockReasoningModel { get; }
    protected MockDataStructuringModel MockDataStructuringModel { get; }
    protected ITextContextualBridge TextContextualBridge { get; }

    protected IntegrationTestBase()
    {
        // Create mock instances
        MockReasoningModel = new MockReasoningModel();
        MockDataStructuringModel = new MockDataStructuringModel();

        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Register logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Register mocks
        services.AddSingleton<IReasoningModel>(MockReasoningModel);
        services.AddSingleton<IDataStructuringModel>(MockDataStructuringModel);
        
        // Register the main service
        services.AddSingleton<ITextContextualBridge, LangBridge.Internal.Infrastructure.ContextualBridging.TextContextualBridge>();

        ServiceProvider = services.BuildServiceProvider();
        
        // Get the bridge for tests
        TextContextualBridge = ServiceProvider.GetRequiredService<ITextContextualBridge>();
    }

    /// <summary>
    /// Resets all mock models to their initial state.
    /// </summary>
    protected void ResetMocks()
    {
        MockReasoningModel.Reset();
        MockDataStructuringModel.Reset();
    }

    /// <summary>
    /// Configures the reasoning model to return "YES" for all feasibility checks.
    /// </summary>
    protected void ConfigureSuccessfulFeasibilityCheck()
    {
        MockReasoningModel.WithResponseForKey("Do we have enough information", "YES: Information available");
    }

    /// <summary>
    /// Configures the reasoning model to return "NO" for all feasibility checks with a specific reason.
    /// </summary>
    protected void ConfigureFailedFeasibilityCheck(string reason = "Not enough information available")
    {
        MockReasoningModel.WithResponseForKey("Do we have enough information", $"NO: {reason}");
    }

    /// <summary>
    /// Configures successful property extraction for simple types.
    /// </summary>
    protected void ConfigureSuccessfulSimpleTypeExtraction(string extractedValue)
    {
        MockReasoningModel.WithResponseForKey("Extract the information", extractedValue);
    }

    /// <summary>
    /// Configures successful property extraction for complex types.
    /// Multiple property values can be provided for different properties.
    /// </summary>
    protected void ConfigureSuccessfulComplexTypeExtraction(params string[] propertyValues)
    {
        MockReasoningModel.WithResponseForKey("Extract the value", propertyValues.FirstOrDefault() ?? "Default Value");
        
        // Configure default responses for multiple properties
        if (propertyValues.Length > 1)
        {
            MockReasoningModel.WithDefaultResponses(propertyValues);
        }
    }

    public virtual void Dispose()
    {
        ServiceProvider?.GetService<ServiceProvider>()?.Dispose();
        GC.SuppressFinalize(this);
    }
}